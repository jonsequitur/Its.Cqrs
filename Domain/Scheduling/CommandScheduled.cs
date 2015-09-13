using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandScheduled : ScheduledCommandResult
    {
        public CommandScheduled(IScheduledCommand command) : base(command)
        {
        }

        public string ClockName { get; set; }

        public override string ToString()
        {
            return "Scheduled" + ClockName.IfNotNullOrEmptyOrWhitespace()
                                          .Then(c => " on clock " + c)
                                          .ElseDefault();
        }
    }
}