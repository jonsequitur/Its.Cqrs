// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Deletes successfully delivered scheduled commands from SQL storage in order to keep database size from growing endlessly. 
    /// </summary>
    /// <seealso cref="IDbMigrator" />
    public class CommandSchedulerCleanupMigration : IDbMigrator
    {
        private readonly DateTimeOffset cutoffDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSchedulerCleanupMigration"/> class.
        /// </summary>
        /// <param name="frequencyInDays">The frequency, in days, at which the cleanup should be performed.</param>
        /// <param name="completedCommandsOlderThan">The age after which completed scheduled commands should be deleted from storage.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public CommandSchedulerCleanupMigration(
            int frequencyInDays,
            TimeSpan completedCommandsOlderThan)
        {
            if (frequencyInDays < 1)
            {
                throw new ArgumentException($"{nameof(frequencyInDays)} must be greater than zero.");
            }

            var now = Domain.Clock.Now();

            cutoffDate = now.Subtract(completedCommandsOlderThan);

            var floor = now.Floor(TimeSpan.FromDays(frequencyInDays));

            MigrationVersion = new Version(floor.Year,
                                           floor.Month,
                                           floor.Day);
        }

        /// <summary>
        /// Gets the scope within of the migration.
        /// </summary>
        /// <remarks>Migrations within one scope are independent of migrations within another scope. Migriation versions are not compared across scopes.</remarks>
        public string MigrationScope { get; } = "CommandSchedulerCleanup";

        /// <summary>
        /// Gets the migration version.
        /// </summary>
        public Version MigrationVersion { get; }

        /// <summary>
        /// Migrates a database using the specified context.
        /// </summary>
        public MigrationResult Migrate(DbContext context)
        {
            var commandSchedulerDbContext = context as CommandSchedulerDbContext;

            if (commandSchedulerDbContext == null)
            {
                throw new ArgumentException($"DbContext should be of type {typeof (CommandSchedulerDbContext)} but was of type {context.GetType()}");
            }

            return Migrate(commandSchedulerDbContext);
        }

        private MigrationResult Migrate(CommandSchedulerDbContext ctx)
        {
            var sql = @"
delete from Scheduler.ScheduledCommand
where AppliedTime is not null and AppliedTime < {0}";

            var numberOfRecords = ctx.Database.ExecuteSqlCommand(
                sql,
                cutoffDate);

            return new MigrationResult
            {
                Log = $"Deleted {numberOfRecords} records\n{string.Format(sql, cutoffDate)}",
                MigrationWasApplied = true
            };
        }
    }
}