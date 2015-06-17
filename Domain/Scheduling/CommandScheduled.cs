using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class CommandScheduled : ICommandSchedulerActivity
    {
        private readonly IScheduledCommand scheduledCommand;

        public CommandScheduled(IScheduledCommand scheduledCommand)
        {
            if (scheduledCommand == null)
            {
                throw new ArgumentNullException("scheduledCommand");
            }
            this.scheduledCommand = scheduledCommand;
        }

        public IScheduledCommand ScheduledCommand
        {
            get
            {
                return scheduledCommand;
            }
        }

        public string ClockName { get; set; }
    }
}