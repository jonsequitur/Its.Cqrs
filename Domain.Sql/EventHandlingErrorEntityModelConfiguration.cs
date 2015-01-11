// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Configures the <see cref="EventHandlingError" /> entity model. 
    /// </summary>
    public class EventHandlingErrorEntityModelConfiguration : IEntityModelConfiguration
    {
        /// <summary>
        ///     Configures the specified configuration registrar.
        /// </summary>
        /// <param name="configurationRegistrar">The configuration registrar.</param>
        public void ConfigureModel(ConfigurationRegistrar configurationRegistrar)
        {
            configurationRegistrar.Add(new EventHandlingErrorEntityTypeConfiguration());
        }

        /// <summary>
        /// Configures the <see cref="EventHandlingError" /> entity model. 
        /// </summary>
        public class EventHandlingErrorEntityTypeConfiguration : EntityTypeConfiguration<EventHandlingError>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EventHandlingErrorEntityTypeConfiguration"/> class.
            /// </summary>
            public EventHandlingErrorEntityTypeConfiguration()
            {
                ToTable("EventHandlingErrors", "Events");
            }
        }
    }
}
