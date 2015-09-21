using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    internal class CommandSchedulerPipeline<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly List<ScheduledCommandInterceptor<TAggregate>> onSchedule = new List<ScheduledCommandInterceptor<TAggregate>>();

        private readonly List<ScheduledCommandInterceptor<TAggregate>> onDeliver = new List<ScheduledCommandInterceptor<TAggregate>>();

        public void OnSchedule(ScheduledCommandInterceptor<TAggregate> segment)
        {
            onSchedule.Insert(0, segment);
        }

        public void OnDeliver(ScheduledCommandInterceptor<TAggregate> segment)
        {
            onDeliver.Insert(0, segment);
        }

        public ICommandScheduler<TAggregate> Compose(Configuration configuration)
        {
            var scheduler = configuration.Container.Resolve<CommandScheduler<TAggregate>>();

            return scheduler
                .Wrap(onSchedule.Compose(),
                      onDeliver.Compose());
        }
    }
}