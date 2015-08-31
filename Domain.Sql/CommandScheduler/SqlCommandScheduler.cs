// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    ///     Schedules commands durably via a SQL backing store for immediate or future application.
    /// </summary>
    public class SqlCommandScheduler :
        CommandSchedulingEventHandler,
        ISchedulerClockTrigger,
        ISchedulerClockRepository
    {
        internal const string DefaultClockName = "default";
        internal readonly SchedulerClockTrigger ClockTrigger;
        private readonly SchedulerClockRepository clockRepository;
        private Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlCommandScheduler" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public SqlCommandScheduler(
            Configuration configuration,
            Func<CommandSchedulerDbContext> createCommandSchedulerDbContext = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.createCommandSchedulerDbContext = createCommandSchedulerDbContext ??
                                                   (() => new CommandSchedulerDbContext());

            var container = configuration.Container;

            var dispatchers = InitializeSchedulersPerAggregateType(
                activity,
                container,
                ClockName);

            base.binders = dispatchers;

            ClockTrigger = new SchedulerClockTrigger(
                this.createCommandSchedulerDbContext,
                async (scheduled, result, db) =>
                {
                    var dispatcher = dispatchers.SingleOrDefault(d => d.AggregateType == scheduled.AggregateType);

                    if (dispatcher != null)
                    {
                        await dispatcher.Deliver(scheduled);
                        result.Add(scheduled.Result);
                    }

                    scheduled.Attempts++;

                    await db.SaveChangesAsync();
                });



            clockRepository = new SchedulerClockRepository(this.createCommandSchedulerDbContext);
        }

        public Func<CommandSchedulerDbContext> CreateCommandSchedulerDbContext
        {
            get
            {
                return createCommandSchedulerDbContext;
            }
            set
            {
                createCommandSchedulerDbContext = value;
            }
        }

        public async Task<SchedulerAdvancedResult> AdvanceClock(string clockName, TimeSpan @by, Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            return await ClockTrigger.AdvanceClock(clockName,
                                                   @by,
                                                   query);
        }

        public async Task<SchedulerAdvancedResult> AdvanceClock(string clockName, DateTimeOffset to, Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            return await ClockTrigger.AdvanceClock(clockName,
                                                   to,
                                                   query);
        }

        public void AssociateWithClock(string clockName, string lookup)
        {
            clockRepository.AssociateWithClock(clockName, lookup);
        }


        public void CreateClock(string clockName, DateTimeOffset startTime)
        {
            clockRepository.CreateClock(clockName, startTime);
        }

        public DateTimeOffset ReadClock(string clockName)
        {
            return clockRepository.ReadClock(clockName);
        }

        public async Task<SchedulerAdvancedResult> Trigger(Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query)
        {
            return await ClockTrigger.Trigger(query);
        }
    }
}