// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command was delivered.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.ScheduledCommandResult" />
    [DebuggerStepThrough]
    public abstract class CommandDelivered : ScheduledCommandResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDelivered"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        protected CommandDelivered(IScheduledCommand command) : base(command)
        {
        }
    }
}