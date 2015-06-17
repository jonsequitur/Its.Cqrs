// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.Its.Domain.Sql.CommandScheduler.Migrations;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandSchedulerDatabaseInitializer : IDatabaseInitializer<CommandSchedulerDbContext>
    {
        public void InitializeDatabase(CommandSchedulerDbContext context)
        {
            var dbMigrator = new DbMigrator(new CommandSchedulerMigrationConfiguration());

            if (dbMigrator.GetPendingMigrations().Any())
            {
                dbMigrator.Update("201407120005564_v0_8_2");

                EnsureDefaultClockExists(context);
            }
        }

        private static void EnsureDefaultClockExists(CommandSchedulerDbContext context)
        {
            var now = Domain.Clock.Now();

            if (!context.Clocks.Any(c => c.Name == SqlCommandScheduler.DefaultClockName))
            {
                context.Clocks.Add(new Clock
                {
                    Name = SqlCommandScheduler.DefaultClockName,
                    StartTime = now,
                    UtcNow = now
                });

                context.SaveChanges();
            }
        }
    }
}