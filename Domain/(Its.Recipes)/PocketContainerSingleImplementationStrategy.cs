using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Domain;

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