using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class CommandScheduler
    {
        public static ICommandScheduler<TAggregate> Wrap<TAggregate>(
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

        public static ICommandScheduler<TAggregate> Create<TAggregate>(
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
            IScheduledCommand<TAggregate> command)
            where TAggregate : class, IEventSourced
        {
            var scheduler = Configuration.Current.CommandScheduler<TAggregate>();
            await scheduler.Deliver(command);
        }

        internal static void DeliverIfPreconditionIsSatisfiedSoon<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            int timeoutInMilliseconds = 10000)
            where TAggregate : class, IEventSourced
        {
            var eventBus = Configuration.Current.EventBus;

            var timeout = TimeSpan.FromMilliseconds(timeoutInMilliseconds);

            eventBus.Events<IEvent>()
                    .Where(
                        e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                             e.ETag == scheduledCommand.DeliveryPrecondition.ETag)
                    .Take(1)
                    .Timeout(timeout)
                    .Subscribe(
                        e => { Task.Run(() =>
                        {
                            Debug.WriteLine("[DeliverIfPreconditionIsSatisfiedSoon] delivering!");
                            return DeliverImmediatelyOnConfiguredScheduler(scheduledCommand);
                        }).Wait(); },
                        onError: ex => { eventBus.PublishErrorAsync(new EventHandlingError(ex)); });
        }
    }
}