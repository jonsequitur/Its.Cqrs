using System;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Provides operations for working with command scheduler clocks.
    /// </summary>
    public interface ISchedulerClockRepository
    {
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
        void AssociateWithClock(string clockName, string lookup);

        /// <summary>
        /// Creates a clock.
        /// </summary>
        /// <param name="clockName">The name of the clock.</param>
        /// <param name="startTime">The initial time to which the clock is set.</param>
        /// <exception cref="System.ArgumentNullException">clockName</exception>
        /// <exception cref="ConcurrencyException">Thrown if a clock with the specified name already exists.</exception>
        void CreateClock(
            string clockName,
            DateTimeOffset startTime);

        DateTimeOffset ReadClock(string clockName);

        string ClockName(IScheduledCommand forCommand);
    }
}