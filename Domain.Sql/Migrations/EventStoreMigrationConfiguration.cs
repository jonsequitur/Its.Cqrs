using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    public sealed class EventStoreMigrationConfiguration<TDbContext> : DbMigrationsConfiguration<TDbContext>
        where TDbContext : DbContext
    {
        public EventStoreMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(TDbContext context)
        {
        }
    }
}