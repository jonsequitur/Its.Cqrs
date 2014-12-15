// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Its.Recipes
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class PocketContainerPrimitiveAvoidanceStrategy
    {
        private static readonly MethodInfo pocketContainerResolveMethod = typeof (PocketContainer).GetMethod("Resolve", new Type[0]);

        private static readonly MethodInfo genericFuncFactoryMethod = typeof (PocketContainerPrimitiveAvoidanceStrategy)
            .GetMethod("UsingLongestConstructorHavingNoPrimitives", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly Type[] primitiveTypes =
        {
            typeof (string),
            typeof (DateTime),
            typeof (DateTimeOffset)
        };

        public static PocketContainer AvoidConstructorsWithPrimitiveTypes(
            this PocketContainer container)
        {
            return container.AddStrategy(UseLongestConstructorHavingNoPrimitiveTypes);
        }

        private static Func<PocketContainer, object> UseLongestConstructorHavingNoPrimitiveTypes(
            Type forType)
        {
            var funcFactory = genericFuncFactoryMethod.MakeGenericMethod(forType);

            var func = funcFactory.Invoke(null, null);

            return (Func<PocketContainer, object>) func;
        }

        private static Func<PocketContainer, T> UsingLongestConstructorHavingNoPrimitives<T>()
        {
            var ctors = typeof (T).GetConstructors()
                                  .OrderByDescending(c => c.GetParameters().Count())
                                  .Where(c => !c.GetParameters().Any(p => p.ParameterType.IsPrimitive()))
                                  .ToArray();

            if (!ctors.Any())
            {
                return null;
            }

            var longestCtorParamCount = ctors.Max(c => c.GetParameters().Count());

            var chosenCtor = ctors.Single(c => c.GetParameters().Length == longestCtorParamCount);

            var container = Expression.Parameter(typeof (PocketContainer), "container");

            var factoryExpr = Expression.Lambda<Func<PocketContainer, T>>(
                Expression.New(chosenCtor,
                               chosenCtor.GetParameters()
                                         .Select(p =>
                                                 Expression.Call(container,
                                                                 pocketContainerResolveMethod
                                                                     .MakeGenericMethod(p.ParameterType)))),
                container);

            return factoryExpr.Compile();
        }

        public static bool IsPrimitive(this Type type)
        {
            return type.IsPrimitive ||
                   primitiveTypes.Contains(type);
        }
    }
}