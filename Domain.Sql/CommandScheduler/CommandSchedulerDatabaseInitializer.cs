using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.Its.Domain.Sql.CommandScheduler.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulerDatabaseInitializer : IDatabaseInitializer<CommandSchedulerDbContext>
    {
        public void InitializeDatabase(CommandSchedulerDbContext context)
        {
            var dbMigrator = new DbMigrator(new CommandSchedulerMigrationConfiguration());
            if (dbMigrator.GetPendingMigrations().Any())
            {
                dbMigrator.Update();
            }
        }
    }
}