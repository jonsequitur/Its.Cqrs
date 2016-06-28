// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Provides in-memory persistence for event sourced aggregates.
    /// </summary>
    public class InMemoryEventSourcedRepository<TAggregate> :
        IEventSourcedRepository<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly InMemoryEventStream eventStream;
        private readonly IEventBus bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventSourcedRepository{TAggregate}"/> class.
        /// </summary>
        public InMemoryEventSourcedRepository(InMemoryEventStream eventStream = null, IEventBus bus = null)
        {
            this.eventStream = eventStream ?? new InMemoryEventStream();
            this.bus = bus ?? new FakeEventBus();
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public async Task<TAggregate> GetLatest(Guid aggregateId)
        {
            if (AggregateType<TAggregate>.SupportsSnapshots)
            {
                var snapshot = await Configuration.Current
                                                  .SnapshotRepository()
                                                  .GetSnapshot(aggregateId);

                if (snapshot != null)
                {
                    return await GetLatestWithSnapshot(aggregateId, snapshot);
                }
            }

            return await GetLatestWithoutSnapshot(aggregateId);
        }

        private async Task<TAggregate> GetLatestWithSnapshot(Guid aggregateId, ISnapshot snapshot)
        {
            var additionalEvents = (await eventStream.All(aggregateId.ToString()))
                .Where(e => e.SequenceNumber > snapshot.Version);

            return AggregateType<TAggregate>.FromSnapshot(
                snapshot,
                additionalEvents.Select(e => e.ToDomainEvent()));
        }

        private async Task<TAggregate> GetLatestWithoutSnapshot(Guid aggregateId)
        {
            var events = (await eventStream.All(aggregateId.ToString())).ToArray();

            if (events.Any())
            {
                return AggregateType<TAggregate>.FromEventHistory.Invoke(
                    aggregateId,
                    events.Select(e => e.ToDomainEvent()));
            }

            return null;
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public async Task<TAggregate> GetVersion(Guid aggregateId, long version)
        {
            var events = (await eventStream.UpToVersion(aggregateId.ToString(), version)).ToArray();

            return events.Any() ? 
                events.CreateAggregate<TAggregate>() : 
                null;
        }

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        public async Task<TAggregate> GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
        {
            var events = (await eventStream.AsOfDate(aggregateId.ToString(), asOfDate)).ToArray();

            return events.Any() ? 
                events.CreateAggregate<TAggregate>() :
                null;
        }

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        public async Task Save(TAggregate aggregate)
        {
            var events = aggregate.PendingEvents.ToArray();

            foreach (var e in events)
            {
                await eventStream.Append(new[]
                {
                    e.ToStoredEvent()
                });

                e.SetAggregate(aggregate);
            }

            var pendingRenames = aggregate.IfTypeIs<IEventMigratingAggregate>()
                .Then(_ => _.PendingRenames)
                .ElseDefault()
                .OrEmpty();

            foreach (var rename in pendingRenames)
            {
                var eventToRename = eventStream.Events.SingleOrDefault(e => e.AggregateId == aggregate.Id.ToString() && e.SequenceNumber == rename.SequenceNumber);
                if (eventToRename == null)
                {
                    throw new EventMigrations.SequenceNumberNotFoundException(aggregate.Id, rename.SequenceNumber);
                }
                eventToRename.Type = rename.NewName;
            }

            aggregate.ConfirmSave();

            // publish the events
            await bus.PublishAsync(events);
        }

        /// <summary>
        ///     Gets a command target by the id.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        async Task<TAggregate> IStore<TAggregate>.Get(string id) => await GetLatest(Guid.Parse(id));

        /// <summary>
        ///     Persists the state of the command target.
        /// </summary>
        async Task IStore<TAggregate>.Put(TAggregate aggregate) => await Save(aggregate);
    }
}