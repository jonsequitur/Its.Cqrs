using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class ScheduledCommandSuccess : ScheduledCommandResult
    {
        public ScheduledCommandSuccess(IScheduledCommand command) : base(command)
        {
        }

        public override bool WasSuccessful
        {
            get
            {
                return true;
            }
        }
    }
}