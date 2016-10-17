// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Provides configuration for a SQL-based command scheduler.
    /// </summary>
    public class CommandSchedulerConfiguration
    {
        private readonly IList<Action<Configuration>> configureActions = new List<Action<Configuration>>();

        /// <summary>
        /// Deletes successfully delivered scheduled commands from SQL storage on a periodic basis in order to keep database size from growing endlessly.
        /// </summary>
        /// <param name="frequencyInDays">The frequency, in days, at which the cleanup should be performed.</param>
        /// <param name="completedCommandsOlderThan">The age after which completed scheduled commands should be deleted from storage.</param>
        /// <returns></returns>
        public CommandSchedulerConfiguration CleanUp(
            int frequencyInDays,
            TimeSpan completedCommandsOlderThan)
        {
            var migration = new CommandSchedulerCleanupMigration(
                frequencyInDays,
                completedCommandsOlderThan);

            Action<Configuration> action = configuration =>
            {
                configuration.QueueBackgroundWork(_ =>
                {
                    using (var db = configuration.Container
                                                 .Resolve<CommandSchedulerDbContext>())
                    {
                        db.EnsureDatabaseIsUpToDate(migration);
                    }
                });
            };

            configureActions.Add(action);

            return this;
        }

        /// <summary>
        /// Specifies the connection string for the command scheduler database.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public CommandSchedulerConfiguration UseConnectionString(
            string connectionString) =>
                UseDbContext(() => new CommandSchedulerDbContext(connectionString));

        /// <summary>
        /// Specifies how to create instances of <see cref="CommandSchedulerDbContext" />.
        /// </summary>
        /// <param name="create">A delegate that creates the db context.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public CommandSchedulerConfiguration UseDbContext(
            Func<CommandSchedulerDbContext> create)
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            configureActions.Add(configuration => configuration.Container.Register(_ => create()));

            return this;
        }

        internal void ApplyTo(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.IsUsingInMemoryCommandScheduling(false);

            configuration
                .Container
                .RegisterDefaultClockName()
                .Register<ISchedulerClockRepository>(
                    c => c.Resolve<SchedulerClockRepository>())
                .Register<IETagChecker>(
                    c => c.Resolve<SqlEventStoreETagChecker>())
#pragma warning disable 618
                .Register<ISchedulerClockTrigger>(
#pragma warning restore 618
                    c => c.Resolve<SchedulerClockTrigger>())
                .RegisterSingle(
                    c => new CommandDelivererResolver(c))
                .Resolve<SqlCommandSchedulerPipelineInitializer>()
                .Initialize(configuration);

            foreach (var configure in configureActions)
            {
                configure(configuration);
            }
        }
    }
}