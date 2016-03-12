// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace Microsoft.Its.Domain.Sql
{
    public class ReadModelInfoEntityModelConfiguration : IEntityModelConfiguration
    {
        public void ConfigureModel(ConfigurationRegistrar configurations) =>
            configurations.Add(new ReadModelInfoEntityTypeConfiguration());

        public class ReadModelInfoEntityTypeConfiguration : EntityTypeConfiguration<ReadModelInfo>
        {
            public ReadModelInfoEntityTypeConfiguration()
            {
                ToTable("ReadModelInfo", "Events");

                HasKey(m => m.Name);

                Property(m => m.Name).HasMaxLength(256);
            }
        }
    }
}