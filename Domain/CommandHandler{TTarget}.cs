using System;
using System.Collections.Concurrent;

namespace Microsoft.Its.Domain
{
    internal static class CommandHandler<TTarget>
    {
        private static readonly ConcurrentDictionary<Type, Type> interfaces = new ConcurrentDictionary<Type, Type>();

        public static Type ForCommandType(Type commandType) =>
            interfaces.GetOrAdd(commandType,
                                t => typeof (ICommandHandler<,>).MakeGenericType(typeof (TTarget), t));
    }
}