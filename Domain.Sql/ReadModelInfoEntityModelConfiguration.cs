// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Configures the entity model for <see cref="ReadModelInfo" />.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.Sql.IEntityModelConfiguration" />
    public class ReadModelInfoEntityModelConfiguration : IEntityModelConfiguration
    {
        /// <summary>
        ///     Configures the specified configuration registrar.
        /// </summary>
        /// <param name="configurations">The configuration registrar.</param>
        public void ConfigureModel(ConfigurationRegistrar configurations) =>
            configurations.Add(new ReadModelInfoEntityTypeConfiguration());

        /// <summary>
        /// Allows configuration to be performed for the <see cref="ReadModelInfo" /> entity.
        /// </summary>
        public class ReadModelInfoEntityTypeConfiguration : EntityTypeConfiguration<ReadModelInfo>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ReadModelInfoEntityTypeConfiguration"/> class.
            /// </summary>
            public ReadModelInfoEntityTypeConfiguration()
            {
                ToTable("ReadModelInfo", "Events");

                HasKey(m => m.Name);

                Property(m => m.Name).HasMaxLength(256);
            }
        }
    }
}