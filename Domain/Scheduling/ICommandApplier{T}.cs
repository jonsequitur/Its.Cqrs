using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal interface ICommandApplier<out TTarget>
    {
        Task ApplyScheduledCommand(IScheduledCommand<TTarget> scheduledCommand, ICommandPreconditionVerifier preconditionVerifier);
    }
}