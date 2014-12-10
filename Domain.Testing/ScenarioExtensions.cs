using System;
using System.Linq;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Methods for working with a <see cref="Scenario" />.
    /// </summary>
    public static class ScenarioExtensions
    {
        /// <summary>
        /// Advances the clock used for setting event timestamps in the scenario.
        /// </summary>
        /// <param name="scenario">The scenario builder.</param>
        /// <param name="timeSpan">The amount of time by which to advance the scenario clock.</param>
        public static Scenario AdvanceClockBy(
            this Scenario scenario, 
            TimeSpan timeSpan)
        {
            VirtualClock.Current.AdvanceBy(timeSpan);
            return scenario;
        }

        /// <summary>
        /// Sets the clock used for setting event timestamps in the scenario.
        /// </summary>
        /// <param name="scenario">The scenario builder.</param>
        /// <param name="time">The time to set the scenario clock to.</param>
        public static Scenario AdvanceClockTo(
            this Scenario scenario,
            DateTimeOffset time)
        { 
            VirtualClock.Current.AdvanceTo(time);
            return scenario;
        }

        /// <summary>
        /// Throws if there have been any event handling errors during the execution of the scenario.
        /// </summary>
        /// <param name="scenario">The scenario.</param>
        public static Scenario VerifyNoEventHandlingErrors(this Scenario scenario)
        {
            if (scenario.EventHandlingErrors.Any())
            {
              throw new AssertionException(
                    "The following event handling errors occurred: " +
                    string.Join("\n", scenario.EventHandlingErrors.Select(e => e.Exception.ToString())));
            }

            return scenario;
        }
    }
}