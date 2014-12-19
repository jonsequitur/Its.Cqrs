// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Pocket;

namespace Microsoft.Its.Domain
{
    internal static class PocketContainerExtensions
    {
        public static PocketContainer UseImmediateCommandScheduling(this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface &&  
                    type.IsGenericType && 
                    type.GetGenericTypeDefinition() == typeof(ICommandScheduler<>))
                {
                    var aggregateType = type.GetGenericArguments().First();
                    var schedulerType = typeof (ImmediateCommandScheduler<>).MakeGenericType(aggregateType);

                    return c => c.Resolve(schedulerType);
                }

                return null;
            });
        }
    }
}