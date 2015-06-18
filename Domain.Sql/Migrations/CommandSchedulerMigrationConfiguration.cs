// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler.Migrations
{
    public sealed class CommandSchedulerMigrationConfiguration : DbMigrationsConfiguration<CommandSchedulerDbContext>
    {
        public CommandSchedulerMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = false;
        }
    }
}