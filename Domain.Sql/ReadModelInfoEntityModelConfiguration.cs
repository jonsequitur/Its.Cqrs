using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace Microsoft.Its.Domain.Sql
{
    public class ReadModelInfoEntityModelConfiguration : IEntityModelConfiguration
    {
        public void ConfigureModel(ConfigurationRegistrar configurations)
        {
            configurations.Add(new ReadModelInfoEntityTypeConfiguration());
        }

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