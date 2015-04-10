// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Newtonsoft.Json;

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

            return AggregateType<TAggregate>.FromEventHistory.Invoke(
                Guid.Parse(id),
                storedEvents.OrderBy(e => e.SequenceNumber)
                            .Select(e => e.ToDomainEvent(streamName))
                            .Where(e => e != null));
        }

    }
}
