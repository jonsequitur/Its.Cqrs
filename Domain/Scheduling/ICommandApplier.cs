using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal interface ICommandApplier<out T>
    {
        Task ApplyScheduledCommand(IScheduledCommand<T> scheduledCommand, ICommandPreconditionVerifier preconditionVerifier);
    }
}