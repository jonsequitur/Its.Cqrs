// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler.Migrations
{
    /// <summary>
    /// Configurations relating to the command scheduler entity model.
    /// </summary>
    public sealed class CommandSchedulerMigrationConfiguration : DbMigrationsConfiguration<CommandSchedulerDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSchedulerMigrationConfiguration"/> class.
        /// </summary>
        public CommandSchedulerMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = false;
        }
    }
}