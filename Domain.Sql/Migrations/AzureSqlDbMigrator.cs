using System;
using System.Data;

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

        public MigrationResult Migrate(IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var sql = $@"
alter database {connection.Database} 
modify (MAXSIZE = {MaxSize},
        EDITION = '{Edition}',
        SERVICE_OBJECTIVE = '{ServiceObjective}')";

            connection.Execute(sql);

            return new MigrationResult
            {
                MigrationWasApplied = true,
                Log = sql
            };
        }
    }
}