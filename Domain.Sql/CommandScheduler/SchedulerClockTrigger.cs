// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
#pragma warning disable 618

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SchedulerClockTrigger : ISchedulerClockTrigger
    {
        private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;

        public SchedulerClockTrigger(
            Func<CommandSchedulerDbContext> createCommandSchedulerDbContext)
        {
            if (createCommandSchedulerDbContext == null)
            {
                throw new ArgumentNullException(nameof(createCommandSchedulerDbContext));
            }
            this.createCommandSchedulerDbContext = createCommandSchedulerDbContext;
        }

        /// <summary>
        /// Advances the clock by a specified amount and triggers any commands that are due by the end of that time period.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="by">The timespan by which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        public async Task<SchedulerAdvancedResult> AdvanceClock(
            string clockName,
            TimeSpan by,
            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null) =>
                await Advance(clockName, by: by, query: query);

        /// <summary>
        /// Advances the clock to a specified time and triggers any commands that are due by that time.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="to">The time to which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        public async Task<SchedulerAdvancedResult> AdvanceClock(
            string clockName,
            DateTimeOffset to,
            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null) =>
                await Advance(clockName, to, query: query);

        private async Task<SchedulerAdvancedResult> Advance(
            string clockName,
            DateTimeOffset? to = null,
            TimeSpan? by = null,
            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException(nameof(clockName));
            }
            if (to == null && by == null)
            {
                throw new ArgumentException($"Either {nameof(to)} or {nameof(by)} must be specified.");
            }

            using (var db = createCommandSchedulerDbContext())
            {
                var clock = await db.Clocks.SingleOrDefaultAsync(c => c.Name == clockName);

                if (clock == null)
                {
                    throw new ObjectNotFoundException($"No clock named {clockName} was found.");
                }

                to = to ?? clock.UtcNow.Add(by.Value);

                if (to < clock.UtcNow)
                {
                    throw new InvalidOperationException($"A clock cannot be moved backward. ({new { Clock = clock.ToJson(), RequestedTime = to }})");
                }

                var result = new SchedulerAdvancedResult(to.Value);

                clock.UtcNow = to.Value;
                await db.SaveChangesAsync();

                var commands = db.ScheduledCommands
                                 .Due(asOf: to)
                                 .Where(c => c.Clock.Id == clock.Id);

                if (query != null)
                {
                    commands = query(commands);
                }

                // ToArray closes the connection so that when we perform saves during the loop there are no connection errors
                foreach (var scheduled in await commands.ToArrayAsync())
                {
                    await Configuration.Current.DeserializeAndDeliver(scheduled, db);
                    result.Add(scheduled.Result);
                }

                return result;
            }
        }
    }
}