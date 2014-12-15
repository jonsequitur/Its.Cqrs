// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web.Routing;
using Microsoft.Practices.Unity;

namespace System.Web.Mvc
{
    [ExcludeFromCodeCoverage]
    internal static class UnityMvcExtensions
    {
        /// <summary>
        ///     Registers types commonly used with ASP.NET MVC with the UnityContainer.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The same container instance.</returns>
        public static IUnityContainer RegisterCommonMvcTypes(this IUnityContainer container)
        {
            container
                .RegisterFactory<HttpContext>(c => HttpContext.Current)
                .RegisterFactory<HttpContextBase>(c => new HttpContextWrapper(HttpContext.Current))
                .RegisterFactory<RouteCollection>(c => RouteTable.Routes)
                .RegisterFactory<GlobalFilterCollection>(c => GlobalFilters.Filters)
                .RegisterFactory<ViewEngineCollection>(c => ViewEngines.Engines)
                .RegisterFactory<ControllerBuilder>(c => ControllerBuilder.Current);

            return container;
        }

        /// <summary>
        ///     Sets up the ASP.NET MVC DependencyResolver to use the specified container instance to resolve dependencies.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The same container instance.</returns>
        public static IUnityContainer UseAsMvcDependencyResolver(this IUnityContainer container)
        {
            DependencyResolver.SetResolver(
                getService: t =>
                {
                    try
                    {
                        return container.Resolve(t);
                    }
                    catch (ResolutionFailedException)
                    {
                        // DependencyResolver expects null when unable to resolve, so it can fall back to type-specific factory implementations.
                        return null;
                    }
                },
                getServices: t => container.ResolveAll(t));

            return container;
        }
    }
}