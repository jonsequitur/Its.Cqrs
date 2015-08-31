using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulingEventHandler : IEventHandler
    {
        protected internal readonly Dictionary<string, Func<ScheduledCommand, Task>> commandDispatchers = new Dictionary<string, Func<ScheduledCommand, Task>>();

        internal ICommandSchedulerDispatcher[] binders;

        protected readonly ISubject<ICommandSchedulerActivity> activity = new Subject<ICommandSchedulerActivity>();

        public Func<IEvent, string> GetClockName = cmd => null;

        /// <summary>
        /// Provides a method so that delegates can point to the always-up-to-date GetClockName implementation, rather than capture a prior version of the delegate.
        /// </summary>
        public string ClockName(IEvent @event)
        {
            if (GetClockName == null)
            {
                return null;
            }

            return GetClockName(@event);
        }

        /// <summary>
        /// An observable of scheduler activity, which is updated each time a command is applied, whether successful or not.
        /// </summary>
        public IObservable<ICommandSchedulerActivity> Activity
        {
            get
            {
                return activity;
            }
        }

        public IEnumerable<IEventHandlerBinder> GetBinders()
        {
            return binders;
        }

        internal static ICommandSchedulerDispatcher[] InitializeSchedulersPerAggregateType(
            ISubject<ICommandSchedulerActivity> subject,
            PocketContainer container,
            Func<IEvent, string> getClockName)
        {
            var binders = AggregateType.KnownTypes
                                       .Select(aggregateType =>
                                       {
                                           var initializerType =
                                               typeof (SchedulerInitializer<>).MakeGenericType(aggregateType);

                                           dynamic initializer = container.Resolve(initializerType);

                                           return (ICommandSchedulerDispatcher) initializer.InitializeScheduler(
                                               subject,
                                               container,
                                               getClockName,
                                               aggregateType);
                                       })
                                       .ToArray();
            return binders;
        }
    }
}