// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for working with the command scheduler.
    /// </summary>
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
            IEvent deliveryDependsOn = null,
            IClock clock = null)
            where TCommand : ICommand<TAggregate> 
            where TAggregate : class
        {
            var scheduledCommand = new ScheduledCommand<TAggregate>(
                command,
                aggregateId,
                dueTime,
                deliveryDependsOn.ToPrecondition(),
                clock);

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
            IPrecondition deliveryDependsOn = null,
            IClock clock = null)
            where TCommand : ICommand<TTarget> 
            where TTarget : class
        {
            var scheduledCommand = new ScheduledCommand<TTarget>(
                command,
                targetId,
                dueTime,
                deliveryDependsOn,
                clock);

            await scheduler.Schedule(scheduledCommand);

            return scheduledCommand;
        }

        /// <summary>
        /// Schedules a constructor command on the specified scheduler.
        /// </summary>
        public static async Task<IScheduledCommand<TTarget>> Schedule<TTarget>(
            this ICommandScheduler<TTarget> scheduler,
            ConstructorCommand<TTarget> command,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null,
            IClock clock = null) 
            where TTarget : class
        {
            return await scheduler.Schedule(
                command: command,
                targetId: command.TargetId ?? command.AggregateId.ToString(),
                dueTime: dueTime,
                deliveryDependsOn: deliveryDependsOn,
                clock: clock);
        }

        internal static ICommandScheduler<TAggregate> InterceptSchedule<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            ScheduledCommandInterceptor<TAggregate> schedule = null)
        {
            schedule = schedule ?? (async (c, next) => await next(c));

            return Create<TAggregate>(
                async command => await schedule(command, async c => await scheduler.Schedule(c)));
        }

        internal static ICommandScheduler<TAggregate> Create<TAggregate>(
            Func<IScheduledCommand<TAggregate>, Task> schedule) =>
                new AnonymousCommandScheduler<TAggregate>(schedule);

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
            where TAggregate : class =>
                await configuration.CommandDeliverer<TAggregate>().Deliver(command);

        internal static void DeliverIfPreconditionIsMetSoon<TAggregate>(
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

        private static EventHasBeenRecordedPrecondition ToPrecondition(this IEvent deliveryDependsOn)
        {
            if (deliveryDependsOn == null)
            {
                return null;
            }

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

            return new EventHasBeenRecordedPrecondition(deliveryDependsOn.ETag, deliveryDependsOn.AggregateId);
        }

        internal const int DefaultNumberOfRetriesOnException = 5;

        internal static bool CommandHandlerDidNotSpecifyRetry(this CommandFailed failure) =>
            failure.RetryAfter == null;

        internal static bool IsRetryableByDefault(this CommandFailed failure) =>
            !failure.IsCanceled &&
            failure.NumberOfPreviousAttempts < DefaultNumberOfRetriesOnException;
    }
}