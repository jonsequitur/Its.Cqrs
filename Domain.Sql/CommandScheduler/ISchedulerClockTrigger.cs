using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public interface ISchedulerClockTrigger
    {
        /// <summary>
        /// Advances the clock by a specified amount and triggers any commands that are due by the end of that time period.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="by">The timespan by which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        Task<SchedulerAdvancedResult> AdvanceClock(
            string clockName,
            TimeSpan by,
            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null);

        /// <summary>
        /// Advances the clock to a specified time and triggers any commands that are due by that time.
        /// </summary>
        /// <param name="clockName">Name of the clock.</param>
        /// <param name="to">The time to which to advance the clock.</param>
        /// <param name="query">A query that can be used to filter the commands to be applied.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        Task<SchedulerAdvancedResult> AdvanceClock(
            string clockName,
            DateTimeOffset to,
            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = null);

        /// <summary>
        /// Triggers all commands matched by the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>
        /// A result summarizing the triggered commands.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        /// <remarks>If the query matches commands that have been successfully applied already or abandoned, they will be re-applied.</remarks>
        Task<SchedulerAdvancedResult> Trigger(Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query);

        /// <summary>
        /// Triggers a specific scheduled command.
        /// </summary>
        /// <param name="scheduled">The scheduled command.</param>
        /// <param name="result">The result of the trigger operation.</param>
        /// <param name="db">The command scheduler database context.</param>
        /// <returns></returns>
        Task Trigger(
            ScheduledCommand scheduled,
            SchedulerAdvancedResult result,
            CommandSchedulerDbContext db);
    }
}