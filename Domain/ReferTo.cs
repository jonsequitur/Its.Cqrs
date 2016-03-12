// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain
{
    public static class ReferTo
    {
        public static CommandReference Command<TCommand>() where TCommand : ICommand =>
            new CommandReference
            {
                CommandName = typeof (TCommand).Name
            };

        public static CommandReference Command<TCommand>(Expression<Func<TCommand, object>> member)
            where TCommand : ICommand => new CommandReference
            {
                CommandName = typeof (TCommand).Name,
                CommandField = member.MemberName()
            };
    }
}