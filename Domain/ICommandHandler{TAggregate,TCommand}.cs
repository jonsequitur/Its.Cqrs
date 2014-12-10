using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Handles commands.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandler<in TAggregate, TCommand>
        where TCommand : class, ICommand<TAggregate>
    {
        /// <summary>
        /// Called when a command has passed validation and authorization checks.
        /// </summary>
        Task EnactCommand(TAggregate aggregate, TCommand command);

        /// <summary>
        /// Handles any exception that occurs during delivery of a scheduled command.
        /// </summary>
        /// <remarks>The aggregate can use this method to control retry and cancelation of the command.</remarks>
        Task HandleScheduledCommandException(TAggregate aggregate, ScheduledCommandFailure<TCommand> command);
    }
}