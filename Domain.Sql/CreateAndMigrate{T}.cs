// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
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

        public CreateAndMigrate() : this(new IDbMigrator[0])
        {
        }
        
        public CreateAndMigrate(params IDbMigrator[] migrators)
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

            // TODO: (InitializeDatabase) support Azure Database customization & wait time

            if (context.Database.Exists())
            {
                if (!context.Database.CompatibleWithModel(false))
                {
                    // QUESTION-JOSEQU: (InitializeDatabase) 
                    Debug.WriteLine("Database is incompatible with entity model for " + typeof (DbContext));
                }
            }
            else
            {
                if (!CreateDatabaseIfNotExists(context))
                {
                    // another concurrent caller created the database, so return and let them run the migrations
                    return;
                }
            }

            context.EnsureDatabaseSchemaIsUpToDate(migrators);
        }

        private static bool CreateDatabaseIfNotExists(TContext context)
        {
            try
            {
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
        }
    }
}