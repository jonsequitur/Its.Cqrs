// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql
{
    public class SqlCommandSchedulerConfiguration
    {
        internal Configuration configuration;

        internal SqlCommandSchedulerConfiguration(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            this.configuration = configuration;
        }

        public SqlCommandSchedulerConfiguration CleanUp(
            int frequencyInDays,
            TimeSpan completedCommandsOlderThan)
        {
            var migration = new CommandSchedulerCleanupMigration(
                frequencyInDays, 
                completedCommandsOlderThan);

            configuration.QueueBackgroundWork(c =>
            {
                using (var db = configuration.Container
                                             .Resolve<CommandSchedulerDbContext>())
                {
                    db.EnsureDatabaseIsUpToDate(migration);
                }
            });

            return this;
        }

        public SqlCommandSchedulerConfiguration UseConnectionString(
            string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }

        internal string ConnectionString { get; set; } = CommandSchedulerDbContext.NameOrConnectionString;
    }
}