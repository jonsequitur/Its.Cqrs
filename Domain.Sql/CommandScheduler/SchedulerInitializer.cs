using System;
using System.Linq;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SchedulerInitializer<T>
        where T : class, IEventSourced
    {
        public ICommandSchedulerDispatcher InitializeScheduler(
            IObserver<ICommandSchedulerActivity> subject,
            PocketContainer container,
            Func<IEvent, string> getClockName,
            Type aggregateType)
        {
            var binder = container.Resolve<SqlCommandSchedulerBinder<T>>();

            var scheduler = binder.Scheduler;

            scheduler.GetClockName = getClockName;
            scheduler.Activity = subject;

            var schedulerType = typeof (ICommandScheduler<T>);

            if (container.All(t => t.Key != schedulerType))
            {
                container.Register(schedulerType, c => scheduler);
            }

            return binder;
        }
    }

    internal interface ICommandSchedulerDispatcher : IEventHandlerBinder
    {
        string AggregateType { get; }

        Task Deliver(ScheduledCommand scheduled);
    }
}