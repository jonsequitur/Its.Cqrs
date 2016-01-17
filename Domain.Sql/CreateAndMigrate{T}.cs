using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    public class CreateAndMigrate<TContext> :
        CreateDatabaseIfNotExists<TContext>
        where TContext : DbContext
    {
        private readonly IDbMigrator[] migrators;

        public CreateAndMigrate(params IDbMigrator[] migrators)
        {
            this.migrators = Migrator.CreateMigratorsFromEmbeddedResourcesFor<TContext>()
                                     .Concat(migrators)
                                     .OrEmpty()
                                     .ToArray();
        }

        public CreateAndMigrate()
        {
            migrators = Migrator.CreateMigratorsFromEmbeddedResourcesFor<TContext>()
                                .ToArray();
        }

        public override void InitializeDatabase(TContext context)
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