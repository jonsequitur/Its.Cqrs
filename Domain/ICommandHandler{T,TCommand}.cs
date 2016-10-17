// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Handles commands that have passed authorization and validation checks.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandler<in TTarget, TCommand>
        where TCommand : class, ICommand<TTarget>
    {
        /// <summary>
        /// Called when a command has passed validation and authorization checks.
        /// </summary>
        Task EnactCommand(TTarget target, TCommand command);

        /// <summary>
        /// Handles any exception that occurs during delivery of a scheduled command.
        /// </summary>
        /// <param name="target">The target of the command.</param>
        /// <param name="command">The command.</param>
        /// <remarks>
        /// The aggregate can use this method to control retry and cancelation of the command.
        /// </remarks>
        Task HandleScheduledCommandException(TTarget target, CommandFailed<TCommand> command);
    }
}
