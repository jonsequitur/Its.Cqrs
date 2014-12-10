using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    public class ScheduledCommandFailure<TCommand> : ScheduledCommandFailure
        where TCommand : class, ICommand
    {
        internal ScheduledCommandFailure(
            TCommand command,
            IScheduledCommand scheduledCommand,
            Exception exception) : base(scheduledCommand, exception)
        {
            Command = command;
        }

        /// <summary>
        /// Gets or sets the scheduled command.
        /// </summary>
        public TCommand Command { get; private set; }
    }
}