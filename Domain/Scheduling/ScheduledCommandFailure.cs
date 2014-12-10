using System;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class ScheduledCommandFailure : ScheduledCommandResult
    {
         internal ScheduledCommandFailure(IScheduledCommand command, Exception exception = null) : base(command)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets or sets the exception that caused the command to fail.
        /// </summary>
        [JsonIgnore]
        public Exception Exception { get; private set; }

        public void Cancel()
        {
            IsCanceled = true;
        }

        public void Retry(TimeSpan after)
        {
            RetryAfter = after;
        }

        public override bool WasSuccessful
        {
            get
            {
                return false;
            }
        }

        internal bool IsCanceled { get; private set; }

        internal TimeSpan? RetryAfter { get; private set; }

        public int NumberOfPreviousAttempts { get; set; }

        internal static ScheduledCommandFailure<TCommand> Create<TCommand>(
            TCommand command,
            IScheduledCommand scheduledCommand,
            Exception exception)
            where TCommand : class, ICommand
        {
            return new ScheduledCommandFailure<TCommand>(command, scheduledCommand, exception);
        }
    }
}