using System;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Provides operations for working with command scheduler clocks.
    /// </summary>
    public interface ISchedulerClockRepository
    {
        /// <summary>
        /// Creates a clock.
        /// </summary>
        /// <param name="clockName">The name of the clock.</param>
        /// <param name="startTime">The initial time to which the clock is set.</param>
        /// <exception cref="System.ArgumentNullException">clockName</exception>
        /// <exception cref="ConcurrencyException">Thrown if a clock with the specified name already exists.</exception>
        IClock CreateClock(
            string clockName,
            DateTimeOffset startTime);

        /// <summary>
        /// Reads the current date and time from the specified clock.
        /// </summary>
        /// <param name="clockName">The name of the clock.</param>
        DateTimeOffset ReadClock(string clockName);

        /// <summary>
        /// Gets the name of clock on which the specified command should be or is scheduled.
        /// </summary>
        /// <param name="forCommand">The command from which to get the name of the clock.</param>
        string ClockName(IScheduledCommand forCommand);
    }
}