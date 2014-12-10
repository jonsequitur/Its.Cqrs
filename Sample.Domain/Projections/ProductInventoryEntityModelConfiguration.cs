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