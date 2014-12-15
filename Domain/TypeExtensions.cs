// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Microsoft.Its.Domain
{
    internal static class TypeExtensions
    {
        internal static IEnumerable<Type> ImplementedHandlerInterfaces(this Type handlerType)
        {
            return handlerType.GetInterfaces()
                              .Where(i => i.IsGenericType)
                              .Where(i => Event.HandlerGenericTypeDefinitions.Contains(i.GetGenericTypeDefinition()));
        }

        public static IEnumerable<Type> KnownEventHandlerTypes(this Type forAggregateType)
        {
            var handlerTypes = (IEnumerable<Type>) (typeof (Event<>).MakeGenericType(forAggregateType)
                                                                    .Member()
                                                                    .KnownHandlerTypes);
            return handlerTypes.Distinct();
        }
    }
}