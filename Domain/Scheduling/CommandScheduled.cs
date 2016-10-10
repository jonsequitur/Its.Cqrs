// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command was successfully scheduled.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.ScheduledCommandResult" />
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandScheduled : ScheduledCommandResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandScheduled"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="clock">The clock on which the command was scheduled.</param>
        public CommandScheduled(IScheduledCommand command, IClock clock = null) : base(command)
        {
            Clock = clock;
        }

        /// <summary>
        /// Gets the clock on which the command was scheduled.
        /// </summary>
        public IClock Clock { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() =>
            $"Scheduled{Clock.IfNotNull().Then(c => " on clock " + c).ElseDefault()}";
    }
}