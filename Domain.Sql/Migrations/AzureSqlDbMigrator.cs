// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    ///     Sets service properties for Azure SQL Databases.
    /// </summary>
    /// <remarks>For details on how to set the properties of this migrator, see https://msdn.microsoft.com/en-us/library/mt574871.aspx</remarks>
    public class AzureSqlDbMigrator : IDbMigrator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSqlDbMigrator"/> class.
        /// </summary>
        /// <param name="serviceObjective">The service objective.</param>
        /// <param name="edition">The edition.</param>
        /// <param name="maxSize">The maximum size.</param>
        /// <param name="migrationVersion">The migration version.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
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

        /// <summary>
        /// Gets the Azure SQL database edition.
        /// </summary>
        public string Edition { get; }

        /// <summary>
        /// Gets the maximum size for the database.
        /// </summary>
        public string MaxSize { get; }

        /// <summary>
        /// Gets the service objective for the database.
        /// </summary>
        public string ServiceObjective { get; }

        /// <summary>
        /// Gets the scope within of the migration.
        /// </summary>
        /// <remarks>Migrations within one scope are independent of migrations within another scope. Migriation versions are not compared across scopes.</remarks>
        public string MigrationScope => "Service";

        /// <summary>
        /// Gets the migration version.
        /// </summary>
        public Version MigrationVersion { get; }

        /// <summary>
        /// Migrates a database using the specified context.
        /// </summary>
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
alter database {context.Database.Connection.Database} 
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