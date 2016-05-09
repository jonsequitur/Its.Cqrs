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
using System.Linq;
using System.Web.Http;
using System.Web.Http.Dependencies;

namespace Pocket
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