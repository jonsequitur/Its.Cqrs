// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Supports writing validations that reference mitigating commands.
    /// </summary>
    public static class ReferTo
    {
        /// <summary>
        /// Creates a reference to a specified command type.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        public static CommandReference Command<TCommand>() where TCommand : ICommand =>
            new CommandReference
            {
                CommandName = typeof(TCommand).Name
            };

        /// <summary>
        /// Creates a reference to a specified command type.
        /// </summary>
        /// <typeparam name="TCommand">The type of the command.</typeparam>
        public static CommandReference Command<TCommand>(Expression<Func<TCommand, object>> member)
            where TCommand : ICommand => new CommandReference
        {
            CommandName = typeof(TCommand).Name,
            CommandField = member.MemberName()
        };
    }
}