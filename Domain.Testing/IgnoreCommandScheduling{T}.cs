using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    internal class IgnoreCommandScheduling<TAggregate> : ICommandScheduler<TAggregate>
        where TAggregate : IEventSourced
    {
        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
        }

        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
        }
    }
}