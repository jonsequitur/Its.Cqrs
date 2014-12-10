using System.Linq;
using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public static class AggregateExtensions
    {
        public static TAggregate SaveToEventStore<TAggregate>(this TAggregate aggregate) where TAggregate : EventSourcedAggregate
        {
            using (var db = new EventStoreDbContext())
            {
                foreach (var e in aggregate.EventHistory.OfType<IEvent<TAggregate>>())
                {
                    var storableEvent = e.ToStorableEvent();
                    db.Events.Add(storableEvent);
                }
                db.SaveChanges();
            }

            return aggregate;
        }
    }
}