// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    internal static class Migrator
    {
        private static readonly string bootstrapResourceName = string.Format("{0}.Migrations.bootstrap-0_0_0_0.sql", typeof (Migrator).Assembly.GetName().Name);

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
                if (exception.Number == 208)
                {
                    return new string[0];
                }

                throw;
            }
        }

        /// <summary>
        /// Ensures that all of the provided migrations have been applied to the database.
        /// </summary>
        /// <typeparam name="TContext">The type of the database context.</typeparam>
        /// <param name="context">The database context specifying which database the migrations are to be applied to.</param>
        /// <param name="migrators">The migrators to apply.</param>
        /// <exception cref="System.ArgumentNullException">migrators</exception>
        public static void EnsureDatabaseSchemaIsUpToDate<TContext>(
            this TContext context,
            params IDbMigrator[] migrators)
            where TContext : DbContext
        {
            if (migrators == null)
            {
                throw new ArgumentNullException("migrators");
            }

            if (!migrators.Any())
            {
                return;
            }

            using (var transaction = new TransactionScope())
            {
                try
                {
                    // don't dispose this connection, since it's managed by the DbContext
                    var connection = context.OpenConnection();

                    var appliedVersions = GetAppliedMigrationVersions(connection);

                    migrators.OrderBy(m => m.MigrationVersion)
                             .Where(m => !appliedVersions.Contains(m.MigrationVersion.ToString()))
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

        private static void ApplyMigration(IDbMigrator migrator, IDbConnection connection)
        {
            var result = migrator.Migrate(connection);

            if (result.MigrationWasApplied)
            {
                connection.Execute(
                    @"INSERT INTO PocketMigrator.AppliedMigrations
             (MigrationVersion
             ,Log
             ,AppliedDate)
     VALUES
            (@migrationVersion,
             @log,
             @appliedDate)",
                    parameters: new Dictionary<string, object>
                    {
                        { "@migrationVersion", migrator.MigrationVersion.ToString() },
                        {
                            "@log",
                            string.Format("{1}\n{0}",
                                          migrator.GetType().AssemblyQualifiedName,
                                          result.Log).Trim()
                        },
                        { "@appliedDate", DateTimeOffset.UtcNow }
                    });
            }
        }

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
                var resourcePrefix = string.Format("{0}.Migrations.{1}-", type.Assembly.GetName().Name, type.Name);

                return type.Assembly
                           .GetManifestResourceNames()
                           .Where(name => name.StartsWith(resourcePrefix,
                                                          StringComparison.InvariantCultureIgnoreCase))
                           .Select(name => new ScriptBasedDbMigrator(name));
            })
                                     .OrderBy(m => m.MigrationVersion)
                                     .ToList();

            // all migrations need the bootstrap migrator
            migrators.Insert(0, new ScriptBasedDbMigrator(bootstrapResourceName));

            return migrators;
        }
    }
}