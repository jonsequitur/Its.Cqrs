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

            var container = configuration.Container;

            container.Register(c => configuration);

            container.RegisterDefaultClockName()
                     .Register<ISchedulerClockRepository>(
                         c => c.Resolve<SchedulerClockRepository>())
                     .Register<IETagChecker>(
                         c => c.Resolve<SqlEventStoreEventStoreETagChecker>())
                     .Register<ISchedulerClockTrigger>(
                         c => c.Resolve<SchedulerClockTrigger>());

            configuration.Container
                         .Resolve<SqlCommandSchedulerPipelineInitializer>()
                         .Initialize(configuration);

            var commandSchedulerResolver = new CommandSchedulerResolver(container);

            container
                .Register(
                    c => new SchedulerClockTrigger(
                             c.Resolve<CommandSchedulerDbContext>,
                             async (serializedCommand, result, db) =>
                             {
                                 await ConfigurationExtensions.DeserializeAndDeliver(commandSchedulerResolver, serializedCommand, db);

                                 result.Add(serializedCommand.Result);
                             }))
                .Register<ISchedulerClockTrigger>(c => c.Resolve<SchedulerClockTrigger>());

            foreach (var configure in configureActions)
            {
                configure(configuration);
            }
        }
    }
}