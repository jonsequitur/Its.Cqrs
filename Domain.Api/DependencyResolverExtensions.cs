// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Web.Http.Dependencies;

namespace Microsoft.Its.Domain.Api
{
    internal static class DependencyResolverExtensions
    {
        private static readonly ConcurrentDictionary<Type, object> resolvers = new ConcurrentDictionary<Type, object>();

        public static T GetOrDiscoverService<T>(this IDependencyResolver resolver, Type type)
        {
            return (T) resolvers.GetOrAdd(type, t =>
            {
                using (var scope = resolver.BeginScope())
                {
                    var service = scope.GetService(type);
                    if (service != null)
                    {
                        return service;
                    }

                    // try to discover the type and instantiate it without the help of the dependency resolver
                    var concreteType = Discover.ConcreteTypesDerivedFrom(type).FirstOrDefault();

                    if (concreteType != null)
                    {
                        return scope.GetService(concreteType) ?? Activator.CreateInstance(type);
                    }
                }
                throw new ArgumentException(string.Format("Could not find any instantiable types derived from {0}. Please register this type using the dependency resolver.", type));
            });
        }
    }
}
