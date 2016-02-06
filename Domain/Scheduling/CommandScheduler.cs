// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class CommandScheduler
    {
        /// <summary>
        /// Schedules a command on the specified scheduler.
        /// </summary>
        public static async Task<IScheduledCommand<TAggregate>> Schedule<TCommand, TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            Guid aggregateId,
            TCommand command,
            DateTimeOffset? dueTime = null,
            IEvent deliveryDependsOn = null)
            where TCommand : ICommand<TAggregate>
        {
            var scheduledCommand = new ScheduledCommand<TAggregate>(
                command,
                aggregateId,
                dueTime,
                deliveryDependsOn.ToPrecondition());

            await scheduler.Schedule(scheduledCommand);

            return scheduledCommand;
        }
        
        /// <summary>
        /// Schedules a command on the specified scheduler.
        /// </summary>
        public static async Task<IScheduledCommand<TTarget>> Schedule<TCommand, TTarget>(
            this ICommandScheduler<TTarget> scheduler,
            string targetId,
            TCommand command,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null)
            where TCommand : ICommand<TTarget>
        {
            var scheduledCommand = new ScheduledCommand<TTarget>(
                command,
                targetId,
                dueTime,
                deliveryDependsOn);

            await scheduler.Schedule(scheduledCommand);

            return scheduledCommand;
        }

        internal static ICommandScheduler<TAggregate> Wrap<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            ScheduledCommandInterceptor<TAggregate> schedule = null,
            ScheduledCommandInterceptor<TAggregate> deliver = null)
        {
            schedule = schedule ?? (async (c, next) => await next(c));
            deliver = deliver ?? (async (c, next) => await next(c));

            return Create<TAggregate>(
                async command => await schedule(command, async c => await scheduler.Schedule(c)),
                async command => await deliver(command, async c => await scheduler.Deliver(c)));
        }

        internal static ICommandScheduler<TAggregate> Create<TAggregate>(
            Func<IScheduledCommand<TAggregate>, Task> schedule,
            Func<IScheduledCommand<TAggregate>, Task> deliver)
        {
            return new AnonymousCommandScheduler<TAggregate>(
                schedule,
                deliver);
        }

        internal static ScheduledCommandInterceptor<TAggregate> Compose<TAggregate>(
            this IEnumerable<ScheduledCommandInterceptor<TAggregate>> pipeline)
        {
            var delegates = pipeline.OrEmpty().ToArray();

            if (!delegates.Any())
            {
                return null;
            }

            return delegates.Aggregate(
                (first, second) =>
                (async (command, next) =>
                 await first(command,
                             async c => await second(c,
                                                     async cc => await next(cc)))));
        }

        internal static async Task DeliverImmediatelyOnConfiguredScheduler<TAggregate>(
            IScheduledCommand<TAggregate> command,
            Configuration configuration)
            where TAggregate : class
        {
            var scheduler = configuration.CommandScheduler<TAggregate>();
            await scheduler.Deliver(command);
        }

        internal static void DeliverIfPreconditionIsSatisfiedSoon<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            Configuration configuration,
            int timeoutInMilliseconds = 10000)
            where TAggregate : class
        {
            Guid aggregateId;
            if (Guid.TryParse(scheduledCommand.DeliveryPrecondition.Scope, out aggregateId))
            {
                var eventBus = configuration.EventBus;

                var timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

                eventBus.Events<IEvent>()
                        .Where(
                            e => e.AggregateId == aggregateId &&
                                 e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                        .Take(1)
                        .Timeout(timeout)
                        .Subscribe(
                            e => Task.Run(() => DeliverImmediatelyOnConfiguredScheduler(scheduledCommand, configuration)).Wait(),
                            onError: ex => eventBus.PublishErrorAsync(new EventHandlingError(ex)));
            }
        }


        private static CommandPrecondition ToPrecondition(this IEvent deliveryDependsOn)
        {
            CommandPrecondition precondition = null;

            if (deliveryDependsOn != null)
            {
                if (deliveryDependsOn.AggregateId == Guid.Empty)
                {
                    throw new ArgumentException("An AggregateId must be set on the event on which the scheduled command depends.");
                }

                if (string.IsNullOrWhiteSpace(deliveryDependsOn.ETag))
                {
                    // set an etag if one is not already assigned
                    deliveryDependsOn.IfTypeIs<Event>()
                                     .ThenDo(e => e.ETag = Guid.NewGuid().ToString("N").ToETag())
                                     .ElseDo(() => { throw new ArgumentException("An ETag must be set on the event on which the scheduled command depends."); });
                }

                precondition = new CommandPrecondition(deliveryDependsOn.ETag, deliveryDependsOn.AggregateId);
            }

            return precondition;
        }

        internal static void DeliverIfPreconditionIsSatisfiedWithin<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            TimeSpan timespan,
            IScheduledCommand<TAggregate> scheduledCommand,
            IEventBus eventBus) where TAggregate : IEventSourced
        {
            Guid aggregateId;
            if (Guid.TryParse(scheduledCommand.DeliveryPrecondition.Scope, out aggregateId))
            {
                eventBus.Events<IEvent>()
                        .Where(
                            e => e.AggregateId == aggregateId &&
                                 e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                        .Take(1)
                        .Timeout(timespan)
                        .Subscribe(
                            e => Task.Run(() => scheduler.Deliver(scheduledCommand)).Wait(),
                            onError: ex => { eventBus.PublishErrorAsync(new EventHandlingError(ex, scheduler)); });
            }
        }

        internal const int DefaultNumberOfRetriesOnException = 5;

        private static readonly MethodInfo createMethod = typeof (CommandFailed)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(m => m.Name == "Create");

        internal static async Task ApplyScheduledCommand<TAggregate>(
            this IStore<TAggregate> store,
            IScheduledCommand<TAggregate> scheduled,
            IETagChecker preconditionChecker = null)
            where TAggregate : class
        {
            TAggregate aggregate = null;
            Exception exception;

            try
            {
                if (preconditionChecker != null &&
                    !await preconditionChecker.IsPreconditionSatisfied(scheduled))
                {
                    await FailScheduledCommand(store,
                                               scheduled,
                                               new PreconditionNotMetException(scheduled.DeliveryPrecondition));
                    return;
                }

                aggregate = await store.Get(scheduled.TargetId);

                if (aggregate == null)
                {
                    if (scheduled.Command is ConstructorCommand<TAggregate>)
                    {
                        var ctor = typeof (TAggregate).GetConstructor(new[] { scheduled.Command.GetType() });

                        if (ctor == null)
                        {
                            throw new InvalidOperationException(string.Format("No constructor was found on type {0} for constructor command {1}.", typeof (TAggregate),
                                                                              scheduled.Command));
                        }

                        aggregate = (TAggregate) ctor.Invoke(new[] { scheduled.Command });
                    }
                    else
                    {
                        throw new PreconditionNotMetException(
                            string.Format("No {0} was found with id {1} so the command could not be applied.",
                                          typeof (TAggregate).Name, scheduled.TargetId));
                    }
                }
                else
                {
                    await aggregate.ApplyAsync(scheduled.Command);
                }

                await store.Put(aggregate);

                scheduled.Result = new CommandSucceeded(scheduled);

                return;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            await FailScheduledCommand(store, scheduled, exception, aggregate);
        }

        private static async Task FailScheduledCommand<TAggregate>(
            IStore<TAggregate> store,
            IScheduledCommand<TAggregate> scheduled,
            Exception exception = null,
            TAggregate aggregate = null)
            where TAggregate : class
        {
            var failure = (CommandFailed) createMethod
                                              .MakeGenericMethod(scheduled.Command.GetType())
                                              .Invoke(null, new object[] { scheduled.Command, scheduled, exception });

            var previousAttempts = scheduled.IfHas<int>(s => s.Metadata.NumberOfPreviousAttempts)
                                            .ElseDefault();

            failure.NumberOfPreviousAttempts = previousAttempts;

            if (aggregate != null)
            {
                var scheduledCommandOfT = scheduled.Command as Command<TAggregate>;
                if (scheduledCommandOfT != null &&
                    scheduledCommandOfT.Handler != null)
                {
                    await scheduledCommandOfT.Handler
                                             .HandleScheduledCommandException((dynamic) aggregate,
                                                                              (dynamic) failure);
                }

                if (exception is ConcurrencyException)
                {
                    if (scheduled.Command is ConstructorCommand<TAggregate>)
                    {
                        // the aggregate has already been created, so this command will never succeed and is redundant.
                        // this may result from redelivery of a constructor command.
                        failure.Cancel();
                        scheduled.Result = failure;
                        return;
                    }

                    // on ConcurrencyException, we don't attempt to save, since it would only result in another ConcurrencyException.
                }
                else
                {
                    try
                    {
                        await store.Put(aggregate);
                    }
                    catch (Exception ex)
                    {
                        // TODO: (FailScheduledCommand) surface this more clearly
                        Trace.Write(ex);
                    }
                }
            }

            if (!failure.IsCanceled &&
                failure.RetryAfter == null &&
                failure.NumberOfPreviousAttempts < DefaultNumberOfRetriesOnException)
            {
                failure.Retry(TimeSpan.FromMinutes(Math.Pow(failure.NumberOfPreviousAttempts + 1, 2)));
            }

            scheduled.Result = failure;
        }
    }
}