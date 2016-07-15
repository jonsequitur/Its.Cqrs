using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Delivers scheduled commands to their targets when they are due.
    /// </summary>
    /// <typeparam name="TTarget">The type of the command target.</typeparam>
    public interface ICommandDeliverer<out TTarget>
    {
        /// <summary>
        /// Delivers the specified scheduled command to the target aggregate.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command to be applied to the aggregate.</param>
        /// <returns>A task that is complete when the command has been applied.</returns>
        /// <remarks>The scheduler will apply the command and save it, potentially triggering additional consequences.</remarks>
        Task Deliver(IScheduledCommand<TTarget> scheduledCommand);
    }
}