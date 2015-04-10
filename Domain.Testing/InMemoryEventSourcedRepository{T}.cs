// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Provides in-memory persistence for event sourced aggregates.
    /// </summary>
    public class InMemoryEventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate> where TAggregate : class, IEventSourced
    {
        private readonly IEventStream eventStream;
        private readonly IEventBus bus;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventSourcedRepository{TAggregate}"/> class.
        /// </summary>
        public InMemoryEventSourcedRepository(IEventStream eventStream = null, IEventBus bus = null)
        {
            this.eventStream = eventStream ?? new InMemoryEventStream(AggregateType<TAggregate>.EventStreamName);
            this.bus = bus ?? new FakeEventBus();
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetLatest(Guid aggregateId)
        {
            if (AggregateType<TAggregate>.SupportsSnapshots)
            {
                var snapshot = Configuration.Current
                                            .SnapshotRepository()
                                            .GetSnapshot(aggregateId)
                                            .Result;

                if (snapshot != null)
                {
                    return GetLatestWithSnapshot(aggregateId, snapshot);
                }
            }

            return GetLatestWithoutSnapshot(aggregateId);
        }

        private TAggregate GetLatestWithSnapshot(Guid aggregateId, ISnapshot snapshot)
        {
            var additionalEvents = eventStream.All(aggregateId.ToString())
                                              .Result
                                              .Where(e => e.SequenceNumber > snapshot.Version);

            return AggregateType<TAggregate>.FromSnapshot(
                snapshot,
                additionalEvents.Select(e => e.ToDomainEvent(AggregateType<TAggregate>.EventStreamName)));
        }

        private TAggregate GetLatestWithoutSnapshot(Guid aggregateId)
        {
            var events = eventStream.All(aggregateId.ToString())
                                    .Result
                                    .ToArray();

            if (events.Any())
            {
                return AggregateType<TAggregate>.FromEventHistory.Invoke(
                    aggregateId,
                    events.Select(e => e.ToDomainEvent(AggregateType<TAggregate>.EventStreamName)));
            }

            return null;
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetVersion(Guid aggregateId, long version)
        {
            var events = eventStream.UpToVersion(aggregateId.ToString(), version)
                                    .Result
                                    .ToArray();

            if (events.Any())
            {
                return events.CreateAggregate<TAggregate>();
            }

            return null;
        }

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        public TAggregate GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
        {
            var events = eventStream.AsOfDate(aggregateId.ToString(), asOfDate)
                                    .Result
                                    .ToArray();

            if (events.Any())
            {
                return events.CreateAggregate<TAggregate>();
            }

            return null;
        }

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        public void Save(TAggregate aggregate)
        {
            var events = aggregate.PendingEvents.ToArray();

            foreach (var e in events)
            {
                eventStream.Append(new[]
                {
                    e.ToStoredEvent()
                }).Wait();

                e.SetAggregate(aggregate);
            }

            // move pending events to the event history
            aggregate.IfTypeIs<EventSourcedAggregate>()
                     .ThenDo(a => a.ConfirmSave());

            // publish the events
            bus.PublishAsync(events)
               .Do(onNext: e => { },
                   onError: ex => Console.WriteLine(ex.ToString()))
               .Wait();
        }

        /// <summary>
        /// Refreshes an aggregate with the latest events from the event stream.
        /// </summary>
        /// <param name="aggregate">The aggregate to refresh.</param>
        /// <remarks>Events not present in the in-memory aggregate will not be re-fetched from the event store.</remarks>
        public void Refresh(TAggregate aggregate)
        {
            var newEvents = eventStream.All(id: aggregate.Id.ToString())
                                       .Result
                                       .Where(e => e.SequenceNumber > aggregate.Version)
                                       .Select(e => e.ToDomainEvent(AggregateType<TAggregate>.EventStreamName));

            aggregate.Update(newEvents);
        }
    }
}