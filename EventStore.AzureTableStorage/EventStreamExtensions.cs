using System;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.EventStore.AzureTableStorage
{
    public static class EventStreamExtensions
    {
        public static void Append(
            this IEventStream eventStream,
            string type, string body, string aggregateId, long? version = null, DateTimeOffset? timestamp = null)
        {
            timestamp = timestamp ?? eventStream
                                         .IfTypeIs<EventStream>()
                                         .Then(es => es.now())
                                         .Else(() => DateTimeOffset.UtcNow);

            var storableEvent = new StoredEvent
            {
                Body = body,
                AggregateId = aggregateId,
                SequenceNumber = (version ?? eventStream.NextVersion(aggregateId)),
                Timestamp = timestamp.Value,
                ClientTimestamp = timestamp.Value,
                Type = type
            };

            eventStream.Append(storableEvent);
        }
    }
}