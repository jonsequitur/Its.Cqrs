using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
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
        public static async Task Done(this SqlCommandScheduler scheduler, string clockName = null)
        {
            clockName = clockName ?? SqlCommandScheduler.DefaultClockName;

            for (var i = 0; i < 10; i++)
            {
                using (var db = scheduler.CreateCommandSchedulerDbContext())
                {
                    var due = db.ScheduledCommands
                                .Due()
                                .Where(c => c.Clock.Name == clockName);

                    if (!await due.AnyAsync())
                    {
                        return;
                    }

                    foreach (var scheduledCommand in await due.ToArrayAsync())
                    {
                        await scheduler.Trigger(scheduledCommand, db);
                    }

                    await Task.Delay(400);
                }
            }
        }
    }
}