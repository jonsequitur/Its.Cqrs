using System;
using System.Data;
using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    ///     Sets service properties for Azure SQL Databases.
    /// </summary>
    /// <remarks>For details on how to set the properties of this migrator, see https://msdn.microsoft.com/en-us/library/mt574871.aspx</remarks>
    public class AzureSqlDbMigrator : IDbMigrator
    {
        public AzureSqlDbMigrator(
            string serviceObjective, 
            string edition,
            string maxSize, 
            Version migrationVersion)
        {
            if (serviceObjective == null)
            {
                throw new ArgumentNullException(nameof(serviceObjective));
            }
            if (maxSize == null)
            {
                throw new ArgumentNullException(nameof(maxSize));
            }
            if (migrationVersion == null)
            {
                throw new ArgumentNullException(nameof(migrationVersion));
            }

            ServiceObjective = serviceObjective;
            Edition = edition;
            MaxSize = maxSize;
            MigrationVersion = migrationVersion;
        }

        public string Edition { get; set; }

        public string MaxSize { get; }

        public string ServiceObjective { get; }

        public string MigrationScope => "Service";

        public Version MigrationVersion { get; }

        public MigrationResult Migrate(DbContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!context.IsAzureDatabase())
            {
                return new MigrationResult
                {
                    MigrationWasApplied = true,
                    Log = "Database is not an Azure SQL database so no action taken."
                };
            }

            var sql =
                $@"
alter database {context.Database} 
modify (MAXSIZE = {MaxSize},
        EDITION = '{Edition}',
        SERVICE_OBJECTIVE = '{ServiceObjective}')";

            context.Database.ExecuteSqlCommand(sql);

            return new MigrationResult
            {
                MigrationWasApplied = true,
                Log = sql
            };
        }
    }
}