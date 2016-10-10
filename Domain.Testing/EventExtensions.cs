// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Methods for working with events.
    /// </summary>
    public static class EventExtensions
    {
        internal static IEnumerable<IEvent> AssignSequenceNumbers(this IEnumerable<IEvent> events)
        {
            // use EventSequences to set SequenceNumbers as needed
            var sequencesPerAggregate = new Dictionary<Guid, EventSequence>();

            return events.Do(e => sequencesPerAggregate.GetOrAdd(e.AggregateId, id => new EventSequence(id)).Add(e));
        }

        /// <summary>
        /// Creates an in-memory stored event from the specified event.
        /// </summary>
        /// <param name="e">The event from which to create an in-memory stored event.</param>
        public static InMemoryStoredEvent ToInMemoryStoredEvent(this IEvent e) =>
            new InMemoryStoredEvent
            {
                SequenceNumber = e.SequenceNumber,
                AggregateId = e.AggregateId.ToString(),
                Timestamp = e.Timestamp,
                Type = e.EventName(),
                Body = e.ToJson(),
                ETag = e.ETag,
                StreamName = e.EventStreamName()
            };

        /// <summary>
        /// Creates an in-memory stored event from the specified <see cref="StorableEvent" />.
        /// </summary>
        /// <param name="e">The event from which to create an in-memory stored event.</param>
        public static InMemoryStoredEvent ToInMemoryStoredEvent(this StorableEvent e) =>
            new InMemoryStoredEvent
            {
                SequenceNumber = e.SequenceNumber,
                AggregateId = e.AggregateId.ToString(),
                Timestamp = e.Timestamp,
                Type = e.Type,
                Body = e.Body,
                ETag = e.ETag,
                StreamName = e.StreamName
            };

        private static readonly Lazy<JsonSerializerSettings> serializerSettings = new Lazy<JsonSerializerSettings>(() =>
        {
            var settings = Serializer.CloneSettings();
            settings.ContractResolver = new OptionalContractResolver();
            return settings;
        });

        /// <summary>
        /// Creates a domain event from an <see cref="InMemoryStoredEvent" />.
        /// </summary>
        /// <param name="storedEvent">The storable event.</param>
        /// <returns>
        /// A deserialized domain event.
        /// </returns>
        public static IEvent ToDomainEvent(this InMemoryStoredEvent storedEvent) =>
            Serializer.DeserializeEvent(
                aggregateName: storedEvent.StreamName,
                eventName: storedEvent.Type,
                aggregateId: Guid.Parse(storedEvent.AggregateId),
                sequenceNumber: storedEvent.SequenceNumber,
                etag: storedEvent.ETag,
                timestamp: storedEvent.Timestamp,
                body: storedEvent.Body,
                uniqueEventId: (long) storedEvent.Metadata.AbsoluteSequenceNumber,
                serializerSettings: serializerSettings.Value);

        /// <summary>
        /// Creates a storable event.
        /// </summary>
        public static StorableEvent ToStorableEvent(this InMemoryStoredEvent storedEvent) =>
            storedEvent.ToDomainEvent().ToStorableEvent();

        /// <summary>
        /// Creates an aggregate from a sequence of stored events.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="events">The events.</param>
        public static TAggregate CreateAggregate<TAggregate>(this IEnumerable<InMemoryStoredEvent> events) where TAggregate : class, IEventSourced
        {
            var storedEvents = events as InMemoryStoredEvent[] ?? events.ToArray();

            var id = storedEvents.Select(e => e.AggregateId).Distinct().Single();

            return AggregateType<TAggregate>.FromEventHistory.Invoke(
                Guid.Parse(id),
                storedEvents.OrderBy(e => e.SequenceNumber)
                            .Select(e => e.ToDomainEvent())
                            .Where(e => e != null));
        }
    }
}