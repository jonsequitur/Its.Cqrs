// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    [DebuggerStepThrough]
    public class SchedulerAdvancedResult
    {
        private readonly ConcurrentBag<ScheduledCommandResult> results = new ConcurrentBag<ScheduledCommandResult>();

        public SchedulerAdvancedResult(DateTimeOffset? now = null)
        {
            Now = now ?? Domain.Clock.Now();
        }

        public DateTimeOffset Now { get; private set; }

        /// <summary>
        /// Gets a summary of the commands that were applied and failed when the scheduler was triggered.
        /// </summary>
        public IEnumerable<CommandFailed> FailedCommands
        {
            get
            {
                return results.OfType<CommandFailed>();
            }
        }

        /// <summary>
        /// Gets a summary of the commands that were successfully applied when the scheduler was triggered.
        /// </summary>
        public IEnumerable<CommandSucceeded> SuccessfulCommands
        {
            get
            {
                return results.OfType<CommandSucceeded>();
            }
        }

        internal void Add(ScheduledCommandResult result)
        {
            results.Add(result);
        }
    }
}
