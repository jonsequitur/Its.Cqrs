// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.Its.Domain
{
    internal static class CommandHandler
    {
        private static readonly Type[] knownTypes = Discover.ConcreteTypesOfGenericInterfaces(typeof(ICommandHandler<,>)).ToArray();

        public static Type Type(Type aggregateType, Type commandType)
        {
            return typeof (ICommandHandler<,>).MakeGenericType(aggregateType, commandType);
        }

        public static Type[] KnownTypes
        {
            get
            {
                return knownTypes;
            }
        }
    }
}