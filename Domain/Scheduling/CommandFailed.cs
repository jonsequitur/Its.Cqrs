// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandFailed : CommandDelivered
    {
        public CommandFailed(IScheduledCommand command, Exception exception = null) : base(command)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets or sets the exception that caused the command to fail.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Cancels the scheduled command. Further delivery attempts will not be made.
        /// </summary>
        public void Cancel()
        {
            IsCanceled = true;
        }

        /// <summary>
        /// Retries the scheduled command after the specified amount of time.
        /// </summary>
        public void Retry(TimeSpan after)
        {
            RetryAfter = after;
        }

        internal bool IsCanceled { get; private set; }

        internal TimeSpan? RetryAfter { get; private set; }

        /// <summary>
        /// Gets or sets the number of previous attempts that have been made to deliver the scheduled command.
        /// </summary>
        public int NumberOfPreviousAttempts { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Failed due to: {1} {0}",
                                 (IsCanceled
                                     ? " (and canceled)"
                                     : " (will retry after " + RetryAfter + ")"), Exception.FindInterestingException().Message);
        }

        internal static CommandFailed Create<TCommand>(
            TCommand command,
            IScheduledCommand scheduledCommand,
            Exception exception)
            where TCommand : class, ICommand
        {
            return new CommandFailed<TCommand>(command, scheduledCommand, exception);
        }
    }
}