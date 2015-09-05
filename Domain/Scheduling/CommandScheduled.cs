using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class CommandScheduled : ScheduledCommandResult
    {
        public CommandScheduled(IScheduledCommand command) : base(command)
        {
        }

        public string ClockName { get; set; }
    }
}