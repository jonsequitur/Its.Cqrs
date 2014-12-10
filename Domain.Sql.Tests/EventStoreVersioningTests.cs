using System.Linq;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class EventStoreVersioningTests : EventStoreDbTest
    {
        [Test]
        public void EventStoreDbContext_v0_8_can_be_used_to_read_events_from_the_migrated_v0_9_schema()
        {
            var aggregateId = Any.Guid();

            Events.Write(
                10,
                i => new Order.CustomerInfoChanged
                {
                    CustomerName = Any.FullName(),
                    ETag = Any.CamelCaseName(),
                    AggregateId = aggregateId,
                    SequenceNumber = i
                },
                createEventStore: () => new EventStoreDbContext());

            using (var eventStore = new EventStoreDbContext_v0_8(EventStoreDbContext.NameOrConnectionString))
            {
                var events = eventStore.Events
                                       .Where(e => e.AggregateId == aggregateId)
                                       .OrderBy(e => e.SequenceNumber);

                events.Select(e => e.SequenceNumber)
                      .Should()
                      .BeEquivalentTo(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                events.Should()
                      .OnlyContain(e => e.ETag == null);
            }
        }
    }
}