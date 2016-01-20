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
            where TAggregate : IEventSourced
        {
            if (aggregateId == Guid.Empty)
            {
                throw new ArgumentException("Parameter aggregateId cannot be an empty Guid.");
            }

            var scheduledCommand = CreateScheduledCommand<TCommand, TAggregate>(
                aggregateId,
                command,
                dueTime,
                deliveryDependsOn);

            await scheduler.Schedule(scheduledCommand);

            return scheduledCommand;
        }

        internal static ICommandScheduler<TAggregate> Wrap<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            ScheduledCommandInterceptor<TAggregate> schedule = null,
            ScheduledCommandInterceptor<TAggregate> deliver = null)
            where TAggregate : IEventSourced
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
            where TAggregate : IEventSourced
        {
            return new AnonymousCommandScheduler<TAggregate>(
                schedule,
                deliver);
        }

        internal static ScheduledCommandInterceptor<TAggregate> Compose<TAggregate>(
            this IEnumerable<ScheduledCommandInterceptor<TAggregate>> pipeline)
            where TAggregate : IEventSourced
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
            where TAggregate : class, IEventSourced
        {
            var scheduler = configuration.CommandScheduler<TAggregate>();
            await scheduler.Deliver(command);
        }

        internal static void DeliverIfPreconditionIsSatisfiedSoon<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            Configuration configuration,
            int timeoutInMilliseconds = 10000)
            where TAggregate : class, IEventSourced
        {
            var eventBus = configuration.EventBus;

            var timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

            eventBus.Events<IEvent>()
                    .Where(
                        e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                             e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                    .Take(1)
                    .Timeout(timeout)
                    .Subscribe(
                        e => { Task.Run(() => DeliverImmediatelyOnConfiguredScheduler(scheduledCommand, configuration)).Wait(); },
                        onError: ex => { eventBus.PublishErrorAsync(new EventHandlingError(ex)); });
        }

        internal static ScheduledCommand<TAggregate> CreateScheduledCommand<TCommand, TAggregate>(
            Guid aggregateId,
            TCommand command,
            DateTimeOffset? dueTime,
            IEvent deliveryDependsOn = null)
            where TCommand : ICommand<TAggregate> where TAggregate : IEventSourced
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
                    deliveryDependsOn.IfTypeIs<Event>()
                                     .ThenDo(e => e.ETag = Guid.NewGuid().ToString("N"))
                                     .ElseDo(() => { throw new ArgumentException("An ETag must be set on the event on which the scheduled command depends."); });
                }

                precondition = new CommandPrecondition
                {
                    AggregateId = deliveryDependsOn.AggregateId,
                    ETag = deliveryDependsOn.ETag
                };
            }

            if (string.IsNullOrEmpty(command.ETag))
            {
                command.IfTypeIs<Command>()
                       .ThenDo(c => c.ETag = CommandContext.Current
                                                           .IfNotNull()
                                                           .Then(ctx => ctx.NextETag(aggregateId.ToString("N")))
                                                           .Else(() => Guid.NewGuid().ToString("N")));
            }

            return new ScheduledCommand<TAggregate>
            {
                Command = command,
                DueTime = dueTime,
                AggregateId = aggregateId,
                SequenceNumber = -DateTimeOffset.UtcNow.Ticks,
                DeliveryPrecondition = precondition
            };
        }

        public static Event<TAggregate> ToEvent<TAggregate>(
            this ScheduledCommand<TAggregate> scheduledCommand)
            where TAggregate : IEventSourced
        {
            return new CommandScheduled<TAggregate>
            {
                Command = scheduledCommand.Command,
                DeliveryPrecondition = scheduledCommand.DeliveryPrecondition,
                SequenceNumber = scheduledCommand.SequenceNumber,
                AggregateId = scheduledCommand.AggregateId,
                DueTime = scheduledCommand.DueTime,
                Result = scheduledCommand.Result
            };
        }

        internal static void DeliverIfPreconditionIsSatisfiedWithin<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            TimeSpan timespan,
            IScheduledCommand<TAggregate> scheduledCommand,
            IEventBus eventBus) where TAggregate : IEventSourced
        {
            eventBus.Events<IEvent>()
                    .Where(
                        e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                             e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                    .Take(1)
                    .Timeout(timespan)
                    .Subscribe(
                        e => { Task.Run(() => scheduler.Deliver(scheduledCommand)).Wait(); },
                        onError: ex => { eventBus.PublishErrorAsync(new EventHandlingError(ex, scheduler)); });
        }

        internal const int DefaultNumberOfRetriesOnException = 5;

        private static readonly MethodInfo createMethod = typeof (CommandFailed)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(m => m.Name == "Create");

        public static async Task ApplyScheduledCommand<TAggregate>(
            this IEventSourcedRepository<TAggregate> repository,
            IScheduledCommand<TAggregate> scheduled,
            ICommandPreconditionVerifier preconditionVerifier = null)
            where TAggregate : class, IEventSourced
        {
            TAggregate aggregate = null;
            Exception exception = null;

            if (scheduled.Result is CommandDelivered)
            {
                return;
            }

            try
            {
                if (preconditionVerifier != null &&
                    !await preconditionVerifier.IsPreconditionSatisfied(scheduled))
                {
                    await FailScheduledCommand(repository,
                                               scheduled,
                                               new PreconditionNotMetException(scheduled.DeliveryPrecondition));
                    return;
                }

                aggregate = await repository.GetLatest(scheduled.AggregateId);

                if (aggregate == null)
                {
                    if (scheduled.Command is ConstructorCommand<TAggregate>)
                    {
                        var ctor = typeof (TAggregate).GetConstructor(new[] { scheduled.Command.GetType() });
                        aggregate = (TAggregate) ctor.Invoke(new[] { scheduled.Command });
                    }
                    else
                    {
                        throw new PreconditionNotMetException(
                            string.Format("No {0} was found with id {1} so the command could not be applied.",
                                          typeof (TAggregate).Name, scheduled.AggregateId), scheduled.AggregateId);
                    }
                }
                else
                {
                    await aggregate.ApplyAsync(scheduled.Command);
                }

                await repository.Save(aggregate);

                scheduled.Result = new CommandSucceeded(scheduled);

                return;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            await FailScheduledCommand(repository, scheduled, exception, aggregate);
        }

        private static async Task FailScheduledCommand<TAggregate>(
            IEventSourcedRepository<TAggregate> repository,
            IScheduledCommand<TAggregate> scheduled,
            Exception exception = null,
            TAggregate aggregate = null)
            where TAggregate : class, IEventSourced
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
                        await repository.Save(aggregate);
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