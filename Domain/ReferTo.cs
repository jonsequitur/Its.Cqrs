using System;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain
{
    public static class ReferTo
    {
        public static CommandReference Command<TCommand>() where TCommand : ICommand
        {
            return new CommandReference
            {
                CommandName = typeof (TCommand).Name,
            };
        }

        public static CommandReference Command<TCommand>(Expression<Func<TCommand, object>> member) where TCommand : ICommand
        {
            return new CommandReference
            {
                CommandName = typeof (TCommand).Name,
                CommandField = member.MemberName()
            };
        }
    }
}