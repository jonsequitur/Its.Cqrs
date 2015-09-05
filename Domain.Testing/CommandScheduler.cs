// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Testing
{
    public static class CommandScheduler
    {
        /// <summary>
        /// Allows awaiting delivery of all commands that are currently due on the command scheduler.
        /// </summary>
        /// <param name="scheduler">The command scheduler.</param>
        /// <param name="clockName">The name of the clock on which the commands are scheduled.</param>
        /// <returns></returns>
        public static async Task Done(
            this ISchedulerClockTrigger scheduler, 
            string clockName = null)
        {
            clockName = clockName ?? SqlCommandScheduler.DefaultClockName;

            for (var i = 0; i < 10; i++)
            {
                using (var db = new CommandSchedulerDbContext())
                {
                    var due = db.ScheduledCommands
                                .Due()
                                .Where(c => c.Clock.Name == clockName);

                    if (!await due.AnyAsync())
                    {
                        return;
                    }

                    var commands = await due.ToArrayAsync();

                    Debug.WriteLine(string.Format("Triggering {0} commands", commands.Count()));

                    foreach (var scheduledCommand in commands)
                    {
                        Debug.WriteLine(string.Format("Triggering {0}:{1}", scheduledCommand.AggregateId, scheduledCommand.SequenceNumber));
                        await scheduler.Trigger(
                            scheduledCommand, 
                            new SchedulerAdvancedResult(), 
                            db);
                    }

                    await Task.Delay(400);
                }
            }

            Debug.WriteLine(string.Format("Done waiting for clock {0}", clockName));
        }

      
    }
}