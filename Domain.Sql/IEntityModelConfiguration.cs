// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity.ModelConfiguration.Configuration;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    ///     Provides a unit of configuration for composed Entity Framework configurations.
    /// </summary>
    public interface IEntityModelConfiguration
    {
        /// <summary>
        ///     Configures the specified configuration registrar.
        /// </summary>
        /// <param name="configurationRegistrar">The configuration registrar.</param>
        void ConfigureModel(ConfigurationRegistrar configurationRegistrar);
    }
}