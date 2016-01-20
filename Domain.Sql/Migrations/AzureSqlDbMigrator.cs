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
            string maxSize, 
            Version migrationVersion)
        {
            if (serviceObjective == null)
            {
                throw new ArgumentNullException("serviceObjective");
            }
            if (maxSize == null)
            {
                throw new ArgumentNullException("maxSize");
            }
            if (migrationVersion == null)
            {
                throw new ArgumentNullException("migrationVersion");
            }

            ServiceObjective = serviceObjective;
            MaxSize = maxSize;
            MigrationVersion = migrationVersion;
        }

        public string ServiceObjective { get; private set; }

        public string MaxSize { get; private set; }

        public string Scope
        {
            get
            {
                return "Service";
            }
        }

        public Version MigrationVersion { get; private set; }

        public MigrationResult Migrate(IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var sql = string.Format(@"
alter database {0} 
modify (MAXSIZE = {1}, 
        SERVICE_OBJECTIVE = '{2}')",
                                    connection.Database,
                                    MaxSize,
                                    ServiceObjective);

            connection.Execute(sql);

            return new MigrationResult
            {
                MigrationWasApplied = true,
                Log = sql
            };
        }
    }
}