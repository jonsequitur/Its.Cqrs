// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Represents the result of a scheduler clock trigger operation.
    /// </summary>
    [DebuggerStepThrough]
    [Obsolete("This class supports ISchedulerClockTrigger which is an obsolete interface.")]
    public class SchedulerAdvancedResult
    {
        private readonly ConcurrentBag<ScheduledCommandResult> results = new ConcurrentBag<ScheduledCommandResult>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerAdvancedResult"/> class.
        /// </summary>
        /// <param name="now">The current time according to the clock being advanced.</param>
        public SchedulerAdvancedResult(DateTimeOffset? now = null)
        {
            Now = now ?? Domain.Clock.Now();
        }

        /// <summary>
        /// Gets the current time according to the clock being advanced.
        /// </summary>
        public DateTimeOffset Now { get; private set; }

        /// <summary>
        /// Gets a summary of the commands that were applied and failed when the scheduler was triggered.
        /// </summary>
        public IEnumerable<CommandFailed> FailedCommands => results.OfType<CommandFailed>();

        /// <summary>
        /// Gets a summary of the commands that were successfully applied when the scheduler was triggered.
        /// </summary>
        public IEnumerable<CommandSucceeded> SuccessfulCommands => results.OfType<CommandSucceeded>();

        internal void Add(ScheduledCommandResult result) => results.Add(result);
    }
}