// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;
using Microsoft.Its.Domain.Sql;

namespace Sample.Domain.Projections
{
    public class ProductInventoryEntityModelConfiguration :
        IEntityModelConfiguration
    {
        public void ConfigureModel(ConfigurationRegistrar configurationRegistrar)
        {
            configurationRegistrar.Add(new ProductInventoryEntityTypeConfig());
        }

        public class ProductInventoryEntityTypeConfig : EntityTypeConfiguration<ProductInventory>
        {
            public ProductInventoryEntityTypeConfig()
            {
                HasKey(rp => rp.ProductName);
            }
        }
    }
}
