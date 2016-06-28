// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet from the PocketContainer project (https://github.com/jonsequitur/PocketContainer). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;

namespace Pocket
{
#if !SourceProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    /// <summary>
    /// Provides support for registration of open generic types.
    /// </summary>
    internal static class PocketContainerOpenGenericStrategy
    {
        /// <summary>
        /// Registers an open generic type to another open generic type, allowing, for example, IService&amp;T&amp; to be registered to resolve to Service&amp;T&amp;.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="variantsOf">The open generic interface that callers will attempt to resolve, e.g. typeof(IService&amp;T&amp;).</param>
        /// <param name="to">The open generic type to resolve, e.g. typeof(Service&amp;T&amp;).</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Parameter 'variantsOf' is not an open generic type, e.g. typeof(IService&amp;T&amp;)
        /// or
        /// Parameter 'to' is not an open generic type, e.g. typeof(Service&amp;T&amp;)
        /// </exception>
        public static PocketContainer RegisterGeneric(this PocketContainer container, Type variantsOf, Type to)
        {
            if (!variantsOf.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Parameter 'variantsOf' is not an open generic type, e.g. typeof(IService<>)");
            }

            if (!to.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Parameter 'to' is not an open generic type, e.g. typeof(Service<>)");
            }

            return container.AddStrategy(t =>
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == variantsOf)
                {
                    var closedGenericType = to.MakeGenericType(t.GetGenericArguments());

                    return c => c.Resolve(closedGenericType);
                }
                return null;
            });
        }
    }
}