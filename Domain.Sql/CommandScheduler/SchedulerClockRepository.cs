// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// A delegate for specifying how to find the name of the clock on which a command is or should be scheduled.
    /// </summary>
    public delegate string GetClockName(IScheduledCommand forCommand);

    internal class SchedulerClockRepository : ISchedulerClockRepository
    {
        private readonly Func<CommandSchedulerDbContext> createDbContext;
        private readonly GetClockName getClockName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerClockRepository"/> class.
        /// </summary>
        /// <param name="createDbContext">The create database context.</param>
        /// <param name="getClockName">Name of the get clock.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
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
        public IClock CreateClock(
            string clockName,
            DateTimeOffset startTime)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException(nameof(clockName));
            }

            using (var db = createDbContext())
            {
                var clock = new Clock
                {
                    Name = clockName,
                    UtcNow = startTime,
                    StartTime = startTime
                };

                db.Clocks.Add(clock);

                try
                {
                    db.SaveChanges();

                    return clock;
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

        /// <summary>
        /// Reads the current date and time from the specified clock.
        /// </summary>
        /// <param name="clockName">The name of the clock.</param>
        public DateTimeOffset ReadClock(string clockName)
        {
            using (var db = createDbContext())
            {
                return db.Clocks.Single(c => c.Name == clockName).UtcNow;
            }
        }

        /// <summary>
        /// Gets the name of clock on which the specified command should be or is scheduled.
        /// </summary>
        /// <param name="forCommand">The command from which to get the name of the clock.</param>
        public string ClockName(IScheduledCommand forCommand) => getClockName(forCommand);
    }
}