// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides lookup and persistence for event sourced aggregates in a SQL database.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    public class SqlEventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly IEventBus bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlEventSourcedRepository{TAggregate}" /> class.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="createEventStoreDbContext">The create event store database context.</param>
        public SqlEventSourcedRepository(
            IEventBus bus = null,
            Func<EventStoreDbContext> createEventStoreDbContext = null)
        {
            this.bus = bus ?? Configuration.Current.EventBus;

            if (createEventStoreDbContext != null)
            {
                GetEventStoreContext = createEventStoreDbContext;
            }
        }

        private async Task<TAggregate> Get(Guid id, long? version = null, DateTimeOffset? asOfDate = null)
        {
            TAggregate aggregate = null;
            ISnapshot snapshot = null;

            if (AggregateType<TAggregate>.SupportsSnapshots)
            {
                snapshot = await Configuration.Current
                                              .SnapshotRepository()
                                              .GetSnapshot(id, version, asOfDate);
            }

            using (var context = GetEventStoreContext())
            {
                var streamName = AggregateType<TAggregate>.EventStreamName;
                var events = context.Events
                                    .Where(e => e.StreamName == streamName)
                                    .Where(x => x.AggregateId == id);

                if (snapshot != null)
                {
                    events = events.Where(e => e.SequenceNumber > snapshot.Version);
                }

                if (version != null)
                {
                    events = events.Where(e => e.SequenceNumber <= version.Value);
                }
                else if (asOfDate != null)
                {
                    var d = asOfDate.Value.UtcDateTime;
                    events = events.Where(e => e.UtcTime <= d);
                }

                var storableEventsList = await events.ToListAsync();
                var domainEventsList = storableEventsList.Select(e => e.ToDomainEvent()).ToList();

                if (snapshot != null)
                {
                    aggregate = AggregateType<TAggregate>.FromSnapshot(snapshot, domainEventsList);
                }
                else if (domainEventsList.Count > 0)
                {
                    aggregate = AggregateType<TAggregate>.FromEventHistory(id, domainEventsList);
                }
            }

            return aggregate;
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public async Task<TAggregate> GetLatest(Guid aggregateId)
        {
            return await Get(aggregateId);
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public async Task<TAggregate> GetVersion(Guid aggregateId, long version)
        {
            return await Get(aggregateId, version: version);
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
            return await Get(aggregateId, asOfDate: asOfDate);
        }

        /// <summary>
        /// Refreshes an aggregate with the latest events from the event stream.
        /// </summary>
        /// <param name="aggregate">The aggregate to refresh.</param>
        /// <remarks>Events not present in the in-memory aggregate will not be re-fetched from the event store.</remarks>
        public async Task Refresh(TAggregate aggregate)
        {
            IEvent[] events;

            using (var context = GetEventStoreContext())
            {
                var streamName = AggregateType<TAggregate>.EventStreamName;

                var storedEvents = await context.Events
                                                .Where(e => e.StreamName == streamName)
                                                .Where(e => e.AggregateId == aggregate.Id)
                                                .Where(e => e.SequenceNumber > aggregate.Version)
                                                .ToArrayAsync();
                events = storedEvents
                    .Select(e => e.ToDomainEvent())
                    .ToArray();
            }

            aggregate.Update(events);
        }

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        /// <exception cref="ConcurrencyException"></exception>
        public async Task Save(TAggregate aggregate)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException("aggregate");
            }

            var events = aggregate.PendingEvents
                                  .Do(e => e.SetAggregate(aggregate))
                                  .ToArray();

            var pendingRenames = aggregate.IfTypeIs<IEventMigratingAggregate>()
                                          .Then(a => a.PendingRenames)
                                          .Else(() => new List<EventMigrations.Rename>())
                                          .ToArray();

            if (!events.Any() && !pendingRenames.Any())
            {
                return;
            }

            var storableEvents = events.OfType<IEvent<TAggregate>>().Select(e =>
            {
                var storableEvent = e.ToStorableEvent();
                storableEvent.StreamName = AggregateType<TAggregate>.EventStreamName;
                return storableEvent;
            }).ToArray();

            using (var context = GetEventStoreContext())
            {
                foreach (var storableEvent in storableEvents)
                {
                    context.Events.Add(storableEvent);
                }

                foreach (var rename in pendingRenames)
                {
                    var renameLocal = rename;
                    var eventToRename = await context.Events
                                                     .SingleOrDefaultAsync(e => e.AggregateId == aggregate.Id &&
                                                                                e.SequenceNumber == renameLocal.SequenceNumber);

                    if (eventToRename == null)
                    {
                        throw new EventMigrations.SequenceNumberNotFoundException(aggregate.Id, rename.SequenceNumber);
                    }

                    eventToRename.Type = rename.NewName;
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    var ids = events.Select(e => e.SequenceNumber).ToArray();

                    var existingEvents = context.Events
                                                .Where(e => e.AggregateId == aggregate.Id)
                                                .Where(e => ids.Any(id => id == e.SequenceNumber))
                                                .ToArray();

                    if (exception.IsConcurrencyException())
                    {
                        var message = string.Format("There was a concurrency violation.\n  Existing:\n {0}\n  Attempted:\n{1}",
                                                    existingEvents.Select(e => e.ToDomainEvent()).ToDiagnosticJson(),
                                                    events.ToDiagnosticJson());
                        throw new ConcurrencyException(message, events, exception);
                    }
                    throw;
                }
            }

            storableEvents.ForEach(storableEvent =>
                                   events.Single(e => e.SequenceNumber == storableEvent.SequenceNumber)
                                         .IfTypeIs<IHaveExtensibleMetada>()
                                         .ThenDo(e => e.Metadata.AbsoluteSequenceNumber = storableEvent.Id));

            aggregate.ConfirmSave();

            // publish the events
            await bus.PublishAsync(events);
        }

        public Func<EventStoreDbContext> GetEventStoreContext = () => new EventStoreDbContext();
    }
}
