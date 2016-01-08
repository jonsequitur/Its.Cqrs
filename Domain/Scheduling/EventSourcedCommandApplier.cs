using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class EventSourcedCommandApplier<TAggregate> : ICanHaveCommandsApplied<TAggregate> where TAggregate : class, IEventSourced
    {
        private readonly IEventSourcedRepository<TAggregate> repository;

        public EventSourcedCommandApplier(IEventSourcedRepository<TAggregate> repository)
        {
            this.repository = repository;
        }

        public async Task ApplyScheduledCommand(IScheduledCommand<TAggregate> scheduledCommand, ICommandPreconditionVerifier preconditionVerifier)
        {
            await repository.ApplyScheduledCommand(scheduledCommand, preconditionVerifier);
        }
    }
}