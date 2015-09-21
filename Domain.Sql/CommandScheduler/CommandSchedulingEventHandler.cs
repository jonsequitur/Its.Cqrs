using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulingEventHandler : IEventHandler
    {
        protected internal readonly Dictionary<string, Func<ScheduledCommand, Task>> commandDispatchers = new Dictionary<string, Func<ScheduledCommand, Task>>();

        internal ICommandSchedulerDispatcher[] binders;

        protected readonly ISubject<ICommandSchedulerActivity> activity = new Subject<ICommandSchedulerActivity>();

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
    }
}