// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command succeeded upon scheduled delivery. 
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.CommandDelivered" />
    [DebuggerStepThrough]
    public class CommandSucceeded : CommandDelivered
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSucceeded"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        public CommandSucceeded(IScheduledCommand command) : base(command)
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => "Succeeded";
    }
}