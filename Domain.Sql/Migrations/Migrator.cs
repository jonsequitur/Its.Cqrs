// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql.Migrations
{
    internal static class Migrator
    {
        private static readonly string bootstrapResourceName = string.Format("{0}.Migrations.bootstrap-0_0_0_0.sql", typeof (Migrator).Assembly.GetName().Name);

        private static string[] GetAppliedMigrationVersions(
            DbConnection connection)
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

        public static void EnsureDatabaseSchemaIsUpToDate<TContext>(
            this TContext context,
            IDbMigrator[] migrators)
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
                             .ForEach(migrator => ApplyMigration<TContext>(migrator, connection));

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

        private static void ApplyMigration<TContext>(IDbMigrator migrator, DbConnection connection) where TContext : DbContext
        {
            var log = migrator.IfTypeIs<ScriptBasedDbMigrator>()
                              .Then(m => m.SqlText)
                              .Else(() => migrator.ToString());

            migrator.Migrate(connection);

            connection.Execute(
                @"INSERT INTO PocketMigrator.AppliedMigrations
             (MigrationVersion
             ,AssemblyVersion
             ,Log
             ,AppliedDate)
     VALUES
            (@migrationVersion,
             @assemblyVersion,
             @log,
             @appliedDate)",
                parameters: new Dictionary<string, object>
                {
                    { "@migrationVersion", migrator.MigrationVersion.ToString() },
                    { "@assemblyVersion", typeof (TContext).Assembly.GetName().Version.ToString() },
                    { "@log", log },
                    { "@appliedDate", DateTimeOffset.UtcNow }
                });
        }

        public static IEnumerable<IDbMigrator> CreateMigratorsFromEmbeddedResourcesFor<TContext>()
            where TContext : DbContext
        {
            var parentage = new List<Type>
            {
                typeof (TContext)
            };

            var baseType = typeof (TContext).BaseType;

            while (baseType != typeof (DbContext))
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