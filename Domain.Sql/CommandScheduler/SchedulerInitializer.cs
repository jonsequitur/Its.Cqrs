using System;
using System.Linq;
using Pocket;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SchedulerInitializer<TAggregate>
        where TAggregate : class, IEventSourced
    {
        public ICommandSchedulerDispatcher InitializeScheduler(
            IObserver<ICommandSchedulerActivity> subject,
            PocketContainer container,
            Func<IEvent, string> getClockName)
        {
            var binder = container.Resolve<SqlCommandSchedulerBinder<TAggregate>>();

            var scheduler = binder.Scheduler;

            scheduler.GetClockName = getClockName;
            scheduler.Activity = subject;

            var schedulerType = typeof (ICommandScheduler<TAggregate>);

            if (container.All(t => t.Key != schedulerType))
            {
                container.Register(schedulerType, c => scheduler);
            }

            return binder;
        }
    }
}