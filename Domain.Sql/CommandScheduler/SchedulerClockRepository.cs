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
        /// Associates an arbitrary lookup string with a named clock.
        /// </summary>
        /// <param name="clockName">The name of the clock.</param>
        /// <param name="lookup">The lookup.</param>
        /// <exception cref="System.ArgumentNullException">
        /// clockName
        /// or
        /// lookup
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the lookup us alreayd associated with another clock.</exception>
        public void AssociateWithClock(string clockName, string lookup)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException(nameof(clockName));
            }
            if (lookup == null)
            {
                throw new ArgumentNullException(nameof(lookup));
            }

            using (var db = createDbContext())
            {
                var clock = db.Clocks.SingleOrDefault(c => c.Name == clockName);

                if (clock == null)
                {
                    var now = Domain.Clock.Now();
                    clock = new Clock
                    {
                        Name = clockName,
                        UtcNow = now,
                        StartTime = now
                    };
                    db.Clocks.Add(clock);
                }

                db.ClockMappings.Add(new ClockMapping
                {
                    Clock = clock,
                    Value = lookup
                });

                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException exception)
                {
                    if (exception.ToString().Contains(@"Cannot insert duplicate key row in object 'Scheduler.ClockMapping' with unique index 'IX_Value'"))
                    {
                        throw new InvalidOperationException($"Value '{lookup}' is already associated with another clock", exception);
                    }
                    throw;
                }
            }
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