// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Indicates that a command failed upon scheduled delivery. 
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <seealso cref="Microsoft.Its.Domain.CommandFailed" />
    [DebuggerStepThrough]
    public class CommandFailed<TCommand> : CommandFailed
        where TCommand : class, ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandFailed{TCommand}"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="scheduledCommand">The scheduled command.</param>
        /// <param name="exception">The exception.</param>
        internal CommandFailed(
            TCommand command,
            IScheduledCommand scheduledCommand,
            Exception exception) : base(scheduledCommand, exception)
        {
            Command = command;
        }

        /// <summary>
        /// Gets or sets the scheduled command.
        /// </summary>
        public TCommand Command { get; private set; }
    }
}
