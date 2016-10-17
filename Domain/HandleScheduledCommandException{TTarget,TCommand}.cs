// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A delegate providing a method for handling exceptions that occurs during delivery of a scheduled command.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="target">The target of the command.</param>
    /// <param name="command">The command.</param>
    /// <remarks>This delegate can be used to control retry and cancelation of the command.</remarks>
    public delegate Task HandleScheduledCommandException<in TTarget, TCommand>(TTarget target, CommandFailed<TCommand> command)
        where TCommand : class, ICommand<TTarget>;
}