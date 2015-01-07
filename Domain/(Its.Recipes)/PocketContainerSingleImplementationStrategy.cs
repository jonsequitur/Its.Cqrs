// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Domain;
using Pocket;

namespace Microsoft.Its.Recipes
{
    [DebuggerStepThrough]
    internal static class PocketContainerSingleImplementationStrategy
    {
        public static PocketContainer IfOnlyOneImplementationUseIt(
            this PocketContainer container)
        {
            return container.AddStrategy(type =>
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    var implementations = Discover.ConcreteTypesDerivedFrom(type)
                                                  .ToArray();

                    if (implementations.Count() == 1)
                    {
                        var implementation = implementations.Single();
                        return c => c.Resolve(implementation);
                    }
                }
                return null;
            });
        }
    }
}
