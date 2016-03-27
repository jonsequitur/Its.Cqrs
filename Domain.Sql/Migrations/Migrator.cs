// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    /// <summary>
    /// Supports database migrations and tasks.
    /// </summary>
    public static class Migrator
    {
        private static readonly string bootstrapResourceName = $"{typeof (Migrator).Assembly.GetName().Name}.Migrations.DbContext-0_0_0_0.sql";

        /// <summary>
        /// Creates database migrators from embedded resources found in the source assembly of <typeparamref name="TContext" />.
        /// </summary>
        public static IEnumerable<IDbMigrator> CreateMigratorsFromEmbeddedResourcesFor<TContext>()
            where TContext : DbContext
        {
            var parentage = new List<Type>
            {
                typeof (TContext)
            };

            var baseType = typeof (TContext).BaseType;

            while (baseType != typeof (DbContext) && baseType != null)
            {
                parentage.Add(baseType);
                baseType = baseType.BaseType;
            }

            var migrators = parentage.SelectMany(type =>
            {
                var resourcePrefix = $"{type.Assembly.GetName().Name}.Migrations.{type.Name}-";

                return type.Assembly
                           .GetManifestResourceNames()
                           .Where(name => name.StartsWith(resourcePrefix,
                                                          StringComparison.InvariantCultureIgnoreCase))
                           .Select(name => new ScriptBasedDbMigrator(name));
            })
                                     .OrderBy(m => m.MigrationVersion)
                                     .ToList();

            migrators.Insert(0, new ScriptBasedDbMigrator(bootstrapResourceName));

            return migrators;
        }

        internal static bool CurrentUserHasWritePermissions<TContext>(this TContext context)
            where TContext : DbContext
        {
            const string HasPermsSql = 
@"SELECT TOP(1) 
HAS_PERMS_BY_NAME(
QUOTENAME(DB_NAME()) + '.' + 
QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' + 
QUOTENAME(name), 
N'OBJECT', 
N'INSERT') 
FROM sys.tables;";
            var result = context.Database.SqlQuery<int?>(HasPermsSql).Single();
            return (result ?? 0) == 1;
        }

        /// <summary>
        /// Ensures that all of the provided migrations have been applied to the database.
        /// </summary>
        /// <typeparam name="TContext">The type of the database context.</typeparam>
        /// <param name="context">The database context specifying which database the migrations are to be applied to.</param>
        /// <param name="migrators">The migrators to apply.</param>
        /// <exception cref="System.ArgumentNullException">migrators</exception>
        public static void EnsureDatabaseIsUpToDate<TContext>(
            this TContext context,
            params IDbMigrator[] migrators)
            where TContext : DbContext
        {
            if (migrators == null)
            {
                throw new ArgumentNullException(nameof(migrators));
            }

            if (!migrators.Any())
            {
                return;
            }

            if (!context.CurrentUserHasWritePermissions())
            {
                return;
            }

            using (var transaction = new TransactionScope())
            using (var appLock = new AppLock(context, "PocketMigrator", false))
            {
                if (!appLock.IsAcquired)
                {
                    return;
                }

                try
                {
                    // don't dispose this connection, since it's managed by the DbContext
                    var connection = context.OpenConnection();

                    var appliedVersions = connection.GetLatestAppliedMigrationVersions()
                                                    .ToDictionary(v => v.MigrationScope,
                                                                  v => v);

                    migrators.OrderBy(m => m.MigrationVersion)
                             .Where(m => appliedVersions.IfContains(m.MigrationScope)
                                                        .Then(a => m.MigrationVersion > a.MigrationVersion)
                                                        .Else(() => true))
                             .ForEach(migrator => ApplyMigration(migrator, connection));

                    transaction.Complete();
                }
                catch (SqlException exception)
                {
                    if (exception.Number != 1205)
                    {
                        // Transaction was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
                        throw;
                    }
                }
            }
        }

        internal static string[] GetAppliedMigrationVersions(this IDbConnection connection)
        {
            try
            {
                return connection
                    .QueryDynamic(
                        @"SELECT MigrationVersion from PocketMigrator.AppliedMigrations")
                    .Single()
                    .Select(x => (string) x.MigrationVersion)
                    .ToArray();
            }
            catch (SqlException exception)
            {
                if (exception.Number == 208) // AppliedMigrations table is not present
                {
                    return new string[0];
                }

                throw;
            }
        }

        internal static AppliedMigration[] GetLatestAppliedMigrationVersions(this IDbConnection connection)
        {
            try
            {
                return connection
                    .QueryDynamic(
                        @"WITH cte AS
(
   SELECT MigrationScope, MigrationVersion,
         ROW_NUMBER() OVER (PARTITION BY MigrationScope ORDER BY Sequence DESC) AS rowNumber
   FROM PocketMigrator.AppliedMigrations
)
SELECT *
FROM cte
WHERE rowNumber = 1")
                    .Single()
                    .Select(x => new AppliedMigration
                    {
                        MigrationScope = x.MigrationScope,
                        MigrationVersion = new Version ((string)x.MigrationVersion)
                    })
                    .ToArray();
            }
            catch (SqlException exception) // AppliedMigrations table is not present
            {
                if (exception.Number == 208)
                {
                    return new AppliedMigration[0];
                }

                throw;
            }
        }

        internal class AppliedMigration
        {
            public string MigrationScope { get; set; }

            public Version MigrationVersion { get; set; }
        }

        private static void ApplyMigration(IDbMigrator migrator, IDbConnection connection)
        {
            var result = migrator.Migrate(connection);

            if (result.MigrationWasApplied)
            {
                connection.Execute(
                    @"INSERT INTO PocketMigrator.AppliedMigrations
             (MigrationScope,
              MigrationVersion,
              Log,
              AppliedDate)
     VALUES
            (@migrationScope, 
             @migrationVersion,
             @log,
             GetDate())",
                    parameters: new Dictionary<string, object>
                    {
                        { "@migrationScope", migrator.MigrationScope },
                        { "@migrationVersion", migrator.MigrationVersion.ToString() },
                        {
                            "@log",
                            $"{migrator.GetType().AssemblyQualifiedName}\n\n{result.Log}".Trim()
                        }
                    });
            }
        }
    }
}