using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Triggers the delivery of scheduled commands.
    /// </summary>
    [Obsolete("The interface will be removed in a future version.")]
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
    }
}