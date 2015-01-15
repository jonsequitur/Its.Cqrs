// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods of discovering types within the application.
    /// </summary>
    public static class Discover
    {
        /// <summary>
        /// Gets concrete types derived from from specified type.
        /// </summary>
        public static IEnumerable<Type> ConcreteTypesDerivedFrom(Type type)
        {
            return AppDomainTypes()
                .Concrete()
                .DerivedFrom(type);
        }

        public static IEnumerable<Type> DerivedFrom(this IEnumerable<Type> types, Type type)
        {
            return types.Where(type.IsAssignableFrom);
        }

        /// <summary>
        /// Gets concrete types based on the specified generic type definition.
        /// </summary>
        public static IEnumerable<Type> ConcreteTypesOfGenericInterfaces(params Type[] types)
        {
            return AppDomainTypes()
                .Concrete()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && types.Contains(i.GetGenericTypeDefinition())));
        }

        /// <summary>
        /// Gets concrete types whose full name matches the specified type name.
        /// </summary>
        /// <remarks>The comparison is case insensitive.</remarks>
        public static IEnumerable<Type> ConcreteTypesNamed(string typeName)
        {
            return AppDomainTypes()
                .Concrete()
                .Where(t => t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the known event handler types for the events of all known aggregate types.
        /// </summary>
        public static IEnumerable<Type> EventHandlerTypes()
        {
            return AggregateType.KnownTypes
                                .SelectMany(t => t.KnownEventHandlerTypes())
                                .Distinct();
        }

        /// <summary>
        /// Gets the known consequenter types for the events of all known aggregate types.
        /// </summary>
        public static IEnumerable<Type> Consequenters()
        {
            return EventHandlerTypes().Where(IsConsequenterType);
        }

        /// <summary>
        /// Determines whether the specified type is an implementation of <see cref="IHaveConsequencesWhen{T}" />.
        /// </summary>
        public static bool IsConsequenterType(this Type type)
        {
            var interfaces = type.GetInterfaces().ToArray();
            return
                interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHaveConsequencesWhen<>)) &&
                !interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IUpdateProjectionWhen<>));
        }

        /// <summary>
        /// Gets the known projector types for the events of all known aggregate types.
        /// </summary>
        public static IEnumerable<Type> ProjectorTypes()
        {
            return EventHandlerTypes().Where(IsProjectorType);
        }

        /// <summary>
        /// Determines whether the specified type is an implementation of <see cref="IUpdateProjectionWhen{T}" />.
        /// </summary>
        public static bool IsProjectorType(this Type type)
        {
            var interfaces = type.GetInterfaces();
            var isEventHandler = (typeof(IEventHandler).IsAssignableFrom(type) ||
                      interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IUpdateProjectionWhen<>)));
            bool isConsequentor = interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHaveConsequencesWhen<>));
            return isEventHandler && !isConsequentor;
        }

        /// <summary>
        /// Gets concrete types, e.g. types that can be instantiated, not interfaces, abstract types, or generic type definitions.
        /// </summary>
        public static IEnumerable<Type> Concrete(this IEnumerable<Type> types)
        {
            return types
                .Where(t => !t.IsAbstract)
                .Where(t => !t.IsInterface)
                .Where(t => !t.IsGenericTypeDefinition);
        }

        public static IEnumerable<Type> AppDomainTypes()
        {
            return AppDomainAssemblies()
                .Where(a => !a.IsDynamic)
                .Where(a => !a.GlobalAssemblyCache)
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetExportedTypes();
                    }
                    catch (TypeLoadException)
                    {
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (FileLoadException)
                    {
                    }
                    return Enumerable.Empty<Type>();
                });
        }

        private static Assembly[] AppDomainAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
    }
}
