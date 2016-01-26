// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    public class CreateAndMigrate<TContext> :
        IDatabaseInitializer<TContext>
        where TContext : DbContext
    {
        private readonly IDbMigrator[] migrators;
        private static bool bypassInitialization;

        public CreateAndMigrate() : this(new IDbMigrator[0])
        {
        }
        
        public CreateAndMigrate(IDbMigrator[] migrators)
        {
            this.migrators = Migrator.CreateMigratorsFromEmbeddedResourcesFor<TContext>()
                                     .Concat(migrators)
                                     .OrEmpty()
                                     .ToArray();
        }

        public void InitializeDatabase(TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (bypassInitialization)
            {
                return;
            }

            if (!context.Database.Exists())
            {
                var created = CreateDatabaseIfNotExists(context);

                if (!created)
                {
                    // another concurrent caller created the database, so return and let them run the migrations
                    return;
                }
            }

            context.EnsureDatabaseSchemaIsUpToDate(migrators);
        }

        private bool CreateDatabaseIfNotExists(TContext context)
        {
            try
            {
                if (context.IsAzureDatabase())
                {
                    // create the database
                    context.CreateAzureDatabase();

                    // this triggers the initializer, which then throws because the schema hasn't been initialized, so we have to suspend initialization momentarily
                    bypassInitialization = true;

                    // create the initial schema
                    var sql = ((IObjectContextAdapter) context)
                        .ObjectContext
                        .CreateDatabaseScript();

                    context.OpenConnection()
                           .Execute(sql);

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
                return false;
            }
            finally
            {
                bypassInitialization = false;
            }
        }
    }
}