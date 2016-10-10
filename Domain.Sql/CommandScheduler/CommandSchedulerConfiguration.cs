// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulerConfiguration
    {
        private readonly IList<Action<Configuration>> configureActions = new List<Action<Configuration>>();

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

        public CommandSchedulerConfiguration UseConnectionString(
            string connectionString) =>
                UseDbContext(() => new CommandSchedulerDbContext(connectionString));

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