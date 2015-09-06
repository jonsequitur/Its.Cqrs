// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
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

        public void Cancel()
        {
            IsCanceled = true;
        }

        public void Retry(TimeSpan after)
        {
            RetryAfter = after;
        }

        internal bool IsCanceled { get; private set; }

        internal TimeSpan? RetryAfter { get; private set; }

        public int NumberOfPreviousAttempts { get; set; }
//        {
//            get
//            {
                // FIX: (NumberOfPreviousAttempts)   return ScheduledCommand.NumberOfPreviousAttempts;
//            }
//        }

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
