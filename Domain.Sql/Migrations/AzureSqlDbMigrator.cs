// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    ///     Sets service properties for Azure SQL Databases.
    /// </summary>
    /// <remarks>
    ///     For details on how to set the properties of this migrator, see
    ///     https://msdn.microsoft.com/en-us/library/mt574871.aspx
    /// </remarks>
    public class AzureSqlDbMigrator : IDbMigrator
    {
        private readonly AzureSqlDatabaseServiceObjective azureSqlDatabaseServiceObjective;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureSqlDbMigrator" /> class.
        /// </summary>
        /// <param name="azureSqlDatabaseServiceObjective"></param>
        /// <param name="migrationVersion">The migration version.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public AzureSqlDbMigrator(
            AzureSqlDatabaseServiceObjective azureSqlDatabaseServiceObjective,
            Version migrationVersion)
        {
            if (azureSqlDatabaseServiceObjective == null)
            {
                throw new ArgumentNullException(nameof(azureSqlDatabaseServiceObjective));
            }
            if (migrationVersion == null)
            {
                throw new ArgumentNullException(nameof(migrationVersion));
            }

            this.azureSqlDatabaseServiceObjective = azureSqlDatabaseServiceObjective;
            MigrationVersion = migrationVersion;
        }

        /// <summary>
        ///     Gets the Azure SQL database edition.
        /// </summary>
        public string Edition => azureSqlDatabaseServiceObjective.Edition;

        /// <summary>
        ///     Gets the maximum size for the database.
        /// </summary>
        public long MaxSize => azureSqlDatabaseServiceObjective.MaxSizeInMegaBytes;


        /// <summary>
        ///     Gets the service objective for the database.
        /// </summary>
        public string ServiceObjective => azureSqlDatabaseServiceObjective.ServiceObjective;

        /// <summary>
        ///     Gets the scope within of the migration.
        /// </summary>
        /// <remarks>
        ///     Migrations within one scope are independent of migrations within another scope. Migriation versions are not
        ///     compared across scopes.
        /// </remarks>
        public string MigrationScope => "Service";

        /// <summary>
        ///     Gets the migration version.
        /// </summary>
        public Version MigrationVersion { get; }

        /// <summary>
        ///     Migrates a database using the specified context.
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
modify (MAXSIZE = {MaxSize}MB,
        EDITION = '{Edition}',
        SERVICE_OBJECTIVE = '{ServiceObjective}')";

            context.Database.ExecuteSqlCommand(sql);

            var sku = context.GetAzureDatabaseProperties();
            var retryCount = 5;
            while (!string.Equals(sku.Edition, Edition, StringComparison.InvariantCultureIgnoreCase)
                   && !string.Equals(sku.Edition, Edition, StringComparison.InvariantCultureIgnoreCase)
                   && retryCount > 0)
            {
                context.Database.Connection.Close();
                Thread.Sleep(TimeSpan.FromSeconds(15));
                try
                {
                    context.OpenConnection();
                    sku = context.GetAzureDatabaseProperties();
                }
                catch (SqlException e)
                {
                    var message = e.Message;
                    if (message.StartsWith("Login failed for user") || message.StartsWith("The connection is broken and recovery is not possible"))
                    {
                        retryCount--;
                        continue;
                    }
                    throw;
                }
            }
            return new MigrationResult
            {
                MigrationWasApplied = true,
                Log = sql
            };
        }
    }
}