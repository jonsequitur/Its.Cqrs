// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A delegate providing a method for enacting a command.
    /// </summary>
    /// <typeparam name="TTarget">The type of the target of the command.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="target">The target of the command.</param>
    /// <param name="command">The command.</param>
    public delegate Task EnactCommand<in TTarget, in TCommand>(TTarget target, TCommand command)
        where TCommand : class, ICommand<TTarget>;
}