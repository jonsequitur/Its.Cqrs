// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Represents the result of a command scheduler operation.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.ICommandSchedulerActivity" />
    [DebuggerStepThrough]
    public abstract class ScheduledCommandResult : ICommandSchedulerActivity
    {
        private readonly IScheduledCommand command;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCommandResult"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        protected ScheduledCommandResult(IScheduledCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            this.command = command;
        }

        /// <summary>
        /// Gets the scheduled command being operated upon.
        /// </summary>
        public IScheduledCommand ScheduledCommand => command;
    }
}
