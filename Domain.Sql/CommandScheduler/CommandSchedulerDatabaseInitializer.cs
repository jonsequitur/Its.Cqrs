// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain.Sql.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulerDatabaseInitializer : CreateAndMigrate<CommandSchedulerDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSchedulerDatabaseInitializer"/> class.
        /// </summary>
        public CommandSchedulerDatabaseInitializer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDatabaseInitializer{TContext}"/> class.
        /// </summary>
        /// <param name="migrators">The migrations to apply during initialization.</param>
        public CommandSchedulerDatabaseInitializer(IDbMigrator[] migrators) : base(migrators)
        {
        }

        protected override bool ShouldRebuildDatabase(CommandSchedulerDbContext context, Version latestVersion) => false;
    }
}