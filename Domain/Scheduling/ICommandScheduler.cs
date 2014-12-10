using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Schedules commands for future execution.
    /// </summary>
    public interface ICommandScheduler<out TAggregate>
        where TAggregate : IEventSourced
    {
        /// <summary>
        /// Schedules the specified command.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command.</param>
        /// <returns>A task that is complete when the command has been successfully scheduled.</returns>
        Task Schedule(IScheduledCommand<TAggregate> scheduledCommand);

        /// <summary>
        /// Delivers the specified scheduled command to the target aggregate.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command to be applied to the aggregate.</param>
        /// <returns>A task that is complete when the command has been applied.</returns>
        /// <remarks>The scheduler will apply the command and save it, potentially triggering additional consequences.</remarks>
        Task Deliver(IScheduledCommand<TAggregate> scheduledCommand);
    }
}