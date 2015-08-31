using System;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SchedulerClockTrigger : ISchedulerClockTrigger
    {
        private readonly Func<CommandSchedulerDbContext> createCommandSchedulerDbContext;
        private readonly Func<ScheduledCommand, SchedulerAdvancedResult, CommandSchedulerDbContext, Task> deliver;

        public SchedulerClockTrigger(
            Func<CommandSchedulerDbContext> createCommandSchedulerDbContext,
            Func<ScheduledCommand, SchedulerAdvancedResult, CommandSchedulerDbContext, Task> deliver)
        {
            if (createCommandSchedulerDbContext == null)
            {
                throw new ArgumentNullException("createCommandSchedulerDbContext");
            }
            if (deliver == null)
            {
                throw new ArgumentNullException("deliver");
            }
            this.createCommandSchedulerDbContext = createCommandSchedulerDbContext;
            this.deliver = deliver;
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
        public async Task<SchedulerAdvancedResult> AdvanceClock(string clockName,
                                                                TimeSpan by,
                                                                Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            return await Advance(clockName, by: @by, query: query);
        }

        /// <summary>
        /// Advances the clock to a specified time and triggers any commands that are due by that time.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="to">The time to which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        public async Task<SchedulerAdvancedResult> AdvanceClock(string clockName,
                                                                DateTimeOffset to,
                                                                Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            return await Advance(clockName, to, query: query);
        }

        private async Task<SchedulerAdvancedResult> Advance(string clockName,
                                                            DateTimeOffset? to = null,
                                                            TimeSpan? by = null,
                                                            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null)
        {
            if (clockName == null)
            {
                throw new ArgumentNullException("clockName");
            }
            if (to == null && @by == null)
            {
                throw new ArgumentException("Either to or by must be specified.");
            }

            using (var db = createCommandSchedulerDbContext())
            {
                var clock = await db.Clocks.SingleOrDefaultAsync(c => c.Name == clockName);

                if (clock == null)
                {
                    throw new ObjectNotFoundException(string.Format("No clock named {0} was found.", clockName));
                }

                to = to ?? clock.UtcNow.Add(@by.Value);

                if (to < clock.UtcNow)
                {
                    throw new InvalidOperationException(string.Format("A clock cannot be moved backward. ({0})", new
                    {
                        Clock = clock.ToJson(),
                        RequestedTime = to
                    }));
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
                    //clock.UtcNow = scheduled.DueTime ?? to.Value;
                    await Trigger(scheduled, result, db);
                }

                return result;
            }
        }

        /// <summary>
        /// Triggers all commands matched by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        /// <remarks>If the query matches commands that have been successfully applied already or abandoned, they will be re-applied.</remarks>
        public async Task<SchedulerAdvancedResult> Trigger(Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query)
        {
            // QUESTION: (Trigger) re: the remarks XML comment, would it be clearer to have two methods, e.g. something like TriggerAnyCommands and TriggerEligibleCommands?
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var result = new SchedulerAdvancedResult();

            using (var db = createCommandSchedulerDbContext())
            {
                var commands = query(db.ScheduledCommands).ToArray();

                foreach (var scheduled in commands)
                {
                    await Trigger(scheduled, result, db);
                }
            }

            return result;
        }

        public async Task Trigger(
            ScheduledCommand scheduled,
            SchedulerAdvancedResult result,
            CommandSchedulerDbContext db)
        {
            await deliver(scheduled, result, db);
        }
    }
}