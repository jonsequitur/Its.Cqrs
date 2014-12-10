// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Linq;
using System.Web.Http.Dependencies;
using Microsoft.Its.Recipes;

namespace System.Web.Http
{
    /// <summary>
    ///     Provides methods for configuring Web Api.
    /// </summary>
    internal static partial class HttpConfigurationExtensions
    {
        /// <summary>
        ///     Configures dependency resolution to use PocketContainer.
        /// </summary>
        /// <param name="configuration">The configuration being configured.</param>
        /// <param name="container">The container to use to resolve dependencies.</param>
        /// <returns>
        ///     The same <see cref="HttpConfiguration" /> instance.
        /// </returns>
        public static HttpConfiguration ResolveDependenciesUsing(
            this HttpConfiguration configuration,
            PocketContainer container)
        {
            configuration.DependencyResolver = new PocketContainerDependencyResolver(container);
            return configuration;
        }
    }
}