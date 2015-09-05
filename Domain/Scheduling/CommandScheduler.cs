using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public static class CommandScheduler
    {
        public static ICommandScheduler<TAggregate> Wrap<TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            ScheduledCommandPipelineDelegate<TAggregate> schedule = null,
            ScheduledCommandPipelineDelegate<TAggregate> deliver = null)
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
    }
}