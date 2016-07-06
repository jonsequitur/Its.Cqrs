// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public delegate string GetClockName(IScheduledCommand forCommand);

    internal class SchedulerClockRepository : ISchedulerClockRepository
    {
        private readonly Func<CommandSchedulerDbContext> createDbContext;
        private readonly GetClockName getClockName;

        public SchedulerClockRepository(
            Func<CommandSchedulerDbContext> createDbContext,
            GetClockName getClockName)
        {
            if (createDbContext == null)
            {
                throw new ArgumentNullException(nameof(createDbContext));
            }
            if (getClockName == null)
            {
                throw new ArgumentNullException(nameof(getClockName));
            }
            this.createDbContext = createDbContext;
            this.getClockName = getClockName;
        }

        /// <summary>
        /// Creates a clock.
        /// </summary>
        /// <param name="clockName">The name of the clock.</param>
        /// <param name="startTime">The initial time to which the clock is set.</param>
        /// <exception cref="System.ArgumentNullException">clockName</exception>
        /// <exception cref="ConcurrencyException">Thrown if a clock with the specified name already exists.</exception>
        public void CreateClock(
            string clockName,
            DateTimeOffset startTime)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException(nameof(clockName));
            }

            using (var db = createDbContext())
            {
                db.Clocks.Add(new Clock
                {
                    Name = clockName,
                    UtcNow = startTime,
                    StartTime = startTime
                });
                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    if (ex.ToString().Contains(@"Cannot insert duplicate key row in object 'Scheduler.Clock' with unique index 'IX_Name'"))
                    {
                        throw new ConcurrencyException($"A clock named '{clockName}' already exists.", innerException: ex);
                    }
                    throw;
                }
            }
        }

        public DateTimeOffset ReadClock(string clockName)
        {
            using (var db = createDbContext())
            {
                return db.Clocks.Single(c => c.Name == clockName).UtcNow;
            }
        }

        public string ClockName(IScheduledCommand forCommand) => getClockName(forCommand);
    }
}