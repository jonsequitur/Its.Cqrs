using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class CommandApplier<T> : ICommandApplier<T> 
    {
        public Task ApplyScheduledCommand(IScheduledCommand<T> scheduledCommand, ICommandPreconditionVerifier preconditionVerifier)
        {
            throw new System.NotImplementedException();
        }
    }
}