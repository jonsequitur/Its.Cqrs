// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command was ignored upon scheduled delivery because it had already been delivered previously. 
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.ScheduledCommandResult" />
    [DebuggerStepThrough]
    [DebuggerDisplay("{ToString()}")]
    public class CommandDeduplicated : ScheduledCommandResult
    {
        private readonly string when;

        internal CommandDeduplicated(IScheduledCommand command, string when) : base(command)
        {
            this.when = when;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"Deduplicated on {when}";
    }
}