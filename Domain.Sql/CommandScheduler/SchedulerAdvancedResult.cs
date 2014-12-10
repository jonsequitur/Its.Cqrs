using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class SchedulerAdvancedResult
    {
        private readonly List<ScheduledCommandResult> results = new List<ScheduledCommandResult>();

        public SchedulerAdvancedResult(DateTimeOffset? now = null)
        {
            Now = now ?? Domain.Clock.Now();
        }

        public DateTimeOffset Now { get; private set; }

        /// <summary>
        /// Gets a summary of the commands that were applied and failed when the scheduler was triggered.
        /// </summary>
        public IEnumerable<ScheduledCommandFailure> FailedCommands
        {
            get
            {
                return results.OfType<ScheduledCommandFailure>();
            }
        }

        /// <summary>
        /// Gets a summary of the commands that were successfully applied when the scheduler was triggered.
        /// </summary>
        public IEnumerable<ScheduledCommandSuccess> SuccessfulCommands
        {
            get
            {
                return results.OfType<ScheduledCommandSuccess>();
            }
        }

        internal void Add(ScheduledCommandResult result)
        {
            results.Add(result);
        }
    }
}