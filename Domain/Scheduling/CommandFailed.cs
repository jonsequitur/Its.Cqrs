// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command failed upon scheduled delivery. 
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.CommandDelivered" />
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandFailed : CommandDelivered
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandFailed"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="exception">The exception, if any, that caused the command to fail.</param>
        public CommandFailed(IScheduledCommand command, Exception exception = null) : base(command)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets or sets the exception that caused the command to fail.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Cancels the scheduled command. Further delivery attempts will not be made.
        /// </summary>
        public void Cancel() => IsCanceled = true;

        /// <summary>
        /// Retries the scheduled command after the specified amount of time.
        /// </summary>
        public void Retry(TimeSpan? after = null) => RetryAfter = after ?? DefaultRetryBackoffPeriod;

        /// <summary>
        /// Gets a value indicating whether the command has been canceled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is canceled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCanceled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the command will be retried.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the command will be retried; otherwise, <c>false</c>.
        /// </value>
        public bool WillBeRetried => RetryAfter != null;

        internal TimeSpan? RetryAfter { get; private set; }

        /// <summary>
        /// Gets or sets the number of previous attempts that have been made to deliver the scheduled command.
        /// </summary>
        public int NumberOfPreviousAttempts => ScheduledCommand.NumberOfPreviousAttempts;

        /// <summary>
        /// Gets the default retry backoff period.
        /// </summary>
        public TimeSpan DefaultRetryBackoffPeriod =>
             TimeSpan.FromMinutes(Math.Pow(NumberOfPreviousAttempts + 1, 2));

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() =>
            string.Format("Failed due to: {1} {0}",
                          IsCanceled
                              ? " (and canceled)"
                              : RetryAfter.IfNotNull()
                                          .Then(r => $" (will retry after {r})")
                                          .Else(() => " (won't retry)"),
                          Exception.FindInterestingException().Message);

        internal static CommandFailed Create<TCommand>(
            TCommand command,
            IScheduledCommand scheduledCommand,
            Exception exception)
            where TCommand : class, ICommand =>
                new CommandFailed<TCommand>(command, scheduledCommand, exception);
    }
}