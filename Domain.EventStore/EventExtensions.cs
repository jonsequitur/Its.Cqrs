using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.EventStore;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.EventStore
{
    public static class EventExtensions
    {
        private static readonly Lazy<JsonSerializerSettings> serializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var settings = Serializer.CloneSettings();
            settings.ContractResolver = new OptionalContractResolver();
            return settings;
        });

        /// <summary>
        /// Creates a domain event from a <see cref="IStoredEvent" />.
        /// </summary>
        /// <param name="storedEvent">The storable event.</param>
        /// <returns>A deserialized domain event.</returns>
        public static IEvent ToDomainEvent(this IStoredEvent storedEvent, string streamName)
        {
            return Serializer.DeserializeEvent(
                streamName,
                storedEvent.Type,
                Guid.Parse(storedEvent.AggregateId),
                storedEvent.SequenceNumber,
                storedEvent.Timestamp, storedEvent.Body,
                storedEvent.Timestamp.Ticks,
                serializerSettings.Value);
        }

        /// <summary>
        /// Creates an aggregate from a sequence of stored events.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="events">The events.</param>
        public static TAggregate CreateAggregate<TAggregate>(this IEnumerable<IStoredEvent> events) where TAggregate : class, IEventSourced
        {
            var streamName = AggregateType<TAggregate>.EventStreamName;

            var storedEvents = events as IStoredEvent[] ?? events.ToArray();

            var id = storedEvents.Select(e => e.AggregateId).Distinct().Single();

            return AggregateType<TAggregate>.Factory.Invoke(
                Guid.Parse(id),
                storedEvents.OrderBy(e => e.SequenceNumber)
                            .Select(e => e.ToDomainEvent(streamName))
                            .Where(e => e != null));
        }
    }
}