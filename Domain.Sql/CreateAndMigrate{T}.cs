// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Creates a database if it doesn't exist, and runs any migrations that have not been applied.
    /// </summary>
    /// <typeparam name="TContext">The type of the database context.</typeparam>
    /// <seealso cref="System.Data.Entity.IDatabaseInitializer{TContext}" />
    public class CreateAndMigrate<TContext> :
        IDatabaseInitializer<TContext>
        where TContext : DbContext
    {
        private readonly IDbMigrator[] migrators;
        private static bool bypassInitialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAndMigrate{TContext}"/> class.
        /// </summary>
        public CreateAndMigrate() : this(new IDbMigrator[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAndMigrate{TContext}"/> class.
        /// </summary>
        /// <param name="migrators">The migrators.</param>
        public CreateAndMigrate(IDbMigrator[] migrators)
        {
            this.migrators = Migrator.CreateMigratorsFromEmbeddedResourcesFor<TContext>()
                                     .Concat(migrators)
                                     .OrEmpty()
                                     .ToArray();
        }

        /// <summary>
        /// Executes the strategy to initialize the database for the given context.
        /// </summary>
        /// <param name="context">The context. </param>
        public void InitializeDatabase(TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (bypassInitialization)
            {
                return;
            }

            // The password on the connection string will be lost as soon as the first query is executed on the context.
            var connectionString = context.Database.Connection.ConnectionString;

            var databaseExists = context.Database.Exists();

            if (databaseExists)
            {
                var databaseVersion = GetDatabaseVersion(context);

                if (ShouldRebuildDatabase(context, databaseVersion))
                {
                    if (context.Database.Connection.State != ConnectionState.Closed)
                    {
                        context.Database.Connection.Close();
                    }
                    context.Database.Delete();
                    databaseExists = false;
                }
            }

            if (!databaseExists)
            {
                var created = CreateDatabaseIfNotExists(context, connectionString);

                if (!created)
                {
                    // another concurrent caller created the database, so return and let them run the migrations
                    return;
                }
            }

            context.EnsureDatabaseIsUpToDate(migrators);
        }

        /// <summary>
        /// Determines whether the database should be rebuilt.
        /// </summary>
        protected virtual bool ShouldRebuildDatabase(
            TContext context, 
            Version latestVersion) => false;

        private static Version GetDatabaseVersion(TContext context)
        {
            var versionStamp = new SetDatabaseVersion<TContext>();

            return context.OpenConnection()
                          .GetLatestAppliedMigrationVersions()
                          .SingleOrDefault(m => m.MigrationScope == versionStamp.MigrationScope)
                          .IfNotNull()
                          .Then(_ => _.MigrationVersion)
                          .ElseDefault();
        }

        private bool CreateDatabaseIfNotExists(TContext context, string connectionString)
        {
            try
            {
                if (context.IsAzureDatabase())
                {
                    // create the database
                    context.CreateAzureDatabase(connectionString: connectionString);

                    // this triggers the initializer, which then throws because the schema hasn't been initialized, so we have to suspend initialization momentarily
                    bypassInitialization = true;

                    // create the initial schema
                    var sql = ((IObjectContextAdapter) context)
                        .ObjectContext
                        .CreateDatabaseScript();

                    try
                    {
                        context.OpenConnection()
                               .Execute(sql);
                    }
                    catch (SqlException exception)
                    {
                        if (exception.Number != 2714) // object already exists
                        {
                            throw;
                        }
                    }

                    return true;
                }

                return context.Database.CreateIfNotExists();
            }
            catch (SqlException exception)
            {
                if (exception.Number == 1801) // database already exists
                {
                    return false;
                }

                throw;
            }
            catch (AutomaticMigrationsDisabledException)
            {
                return true;
            }
            finally
            {
                bypassInitialization = false;
            }
        }
    }
}