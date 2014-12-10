using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class CommandPreconditionVerifier<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly Func<EventStoreDbContext> createEventStoreDbContext;

        public CommandPreconditionVerifier(Func<EventStoreDbContext> createEventStoreDbContext)
        {
            if (createEventStoreDbContext == null)
            {
                throw new ArgumentNullException("createEventStoreDbContext");
            }
            this.createEventStoreDbContext = createEventStoreDbContext;
        }

        public async Task<bool> VerifyPrecondition(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand == null)
            {
                throw new ArgumentNullException("scheduledCommand");
            }

            if (scheduledCommand.DeliveryPrecondition == null)
            {
                return true;
            }

            using (var eventStore = createEventStoreDbContext())
            {
                var preconditionMet =
                    eventStore.Events.Any(
                        e => e.AggregateId == scheduledCommand.DeliveryPrecondition.AggregateId &&
                             e.ETag == scheduledCommand.DeliveryPrecondition.ETag);

                return preconditionMet;
            }
        }
    }
}