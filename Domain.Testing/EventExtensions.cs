using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.EventStore;

namespace Microsoft.Its.Domain.Testing
{
    public static class EventExtensions
    {
        public static IEventStream ToEventStream(this IEnumerable<IEvent> events, string name)
        {
            var stream = new InMemoryEventStream(name);

            var storableEvents = events.AssignSequenceNumbers()
                                       .Select(e => e.ToStoredEvent());

            stream.Append(storableEvents.ToArray())
                  .Wait();

            return stream;
        }

        public static IEnumerable<IEvent> AssignSequenceNumbers(this IEnumerable<IEvent> events)
        {
            // use EventSequences to set SequenceNumbers as needed
            var sequencesPerAggregate = new Dictionary<Guid, EventSequence>();

            return events.Do(e => sequencesPerAggregate.GetOrAdd(e.AggregateId, id => new EventSequence(id)).Add(e));
        }

        public static IStoredEvent ToStoredEvent(this IEvent e)
        {
            return new InMemoryStoredEvent
            {
                SequenceNumber = e.SequenceNumber,
                AggregateId = e.AggregateId.ToString(),
                Timestamp = e.Timestamp,
                Type = e.EventName(),
                Body = e.ToJson(),
                ETag = e.ETag
            };
        }

        internal static InMemoryStoredEvent ToStoredEvent(this IStoredEvent e)
        {
            return new InMemoryStoredEvent
            {
                SequenceNumber = e.SequenceNumber,
                AggregateId = e.AggregateId,
                Timestamp = e.Timestamp,
                Type = e.Type,
                Body = e.Body,
                ETag = e.ETag
            };
        }
    }
}