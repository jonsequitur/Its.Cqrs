// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Allows a single reader among distributed instances to query and iterate events from the event store in order of their id, ascending.
    /// </summary>
    internal class ExclusiveEventStoreCatchupQuery : IDisposable
    {
        private readonly EventStoreDbContext dbContext;
        private readonly string lockResourceName;
        private readonly SerialDisposable appLockDisposer = new SerialDisposable();
        private readonly long expectedNumberOfEvents;
        private readonly IEnumerable<StorableEvent> events;
        private readonly long startAtId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveEventStoreCatchupQuery" /> class.
        /// </summary>
        /// <param name="dbContext">The event store database context to execute the query against.</param>
        /// <param name="lockResourceName">Name of the lock. Multiple instances compete with other instances having the same <paramref name="lockResourceName" />.</param>
        /// <param name="getStartAtId">The id of the first event to query.</param>
        /// <param name="matchEvents">Specifies the event types to include the query. If none are specified, all events are queried.</param>
        /// <param name="batchSize">The number of events queried from the event store at each iteration.</param>
         /// <param name="filter">An optional filter expression to constrain the query that the catchup uses over the event store.</param>
        public ExclusiveEventStoreCatchupQuery(
            EventStoreDbContext dbContext,
            string lockResourceName, 
            Func<long> getStartAtId, 
            MatchEvent[] matchEvents,
            int batchSize = 10000,
            Expression<Func<StorableEvent, bool>> filter = null)
        {
            if (batchSize < 1)
            {
                throw new ArgumentException($"{nameof(batchSize)} must be greater than zero.");
            }

            this.dbContext = dbContext;
            this.lockResourceName = lockResourceName;

            if (TryGetAppLock())
            {
                startAtId = getStartAtId();
                IQueryable<StorableEvent> eventQuery = dbContext.Events.AsNoTracking();

                matchEvents = matchEvents ?? new[] { new MatchEvent() };

                // if specific event types are requested, we can optimize the event store query
                // if Event or IEvent are requested, we don't filter -- this requires reading every event
                if (matchEvents.Any())
                {
                    var eventTypes = matchEvents.Select(m => m.Type).Distinct().ToArray();
                    var aggregates = matchEvents.Select(m => m.StreamName).Distinct().ToArray();

                    if (!aggregates.Any(streamName => string.IsNullOrWhiteSpace(streamName) || streamName == MatchEvent.Wildcard))
                    {
                        if (!eventTypes.Any(type => string.IsNullOrWhiteSpace(type) || type == MatchEvent.Wildcard))
                        {
                            // Filter on StreamName and Type
                            var projectionEventFilter = new CatchupEventFilter(matchEvents);
                            eventQuery = eventQuery.Where(projectionEventFilter.Filter);
                        }
                        else
                        {
                            // Filter on StreamName
                            eventQuery = eventQuery.Where(e => aggregates.Contains(e.StreamName));
                        }
                    }
                }

                if (filter != null)
                {
                    eventQuery = eventQuery.Where(filter);
                }

                eventQuery = eventQuery
                    .Where(e => e.Id >= startAtId)
                    .Take(batchSize);
                
                expectedNumberOfEvents = eventQuery.Count();

                events = DurableStreamFrom(eventQuery, startAtId);
            }
            else
            {
                events = Enumerable.Empty<StorableEvent>();
            }
        }

        public virtual IEnumerable<StorableEvent> Events => events;

        public long ExpectedNumberOfEvents => expectedNumberOfEvents;

        public long StartAtId => startAtId;

        private bool TryGetAppLock()
        {
            var appLock = new AppLock(dbContext, lockResourceName);
            appLockDisposer.Disposable = appLock;
            return appLock.IsAcquired;
        }

        internal IEnumerable<StorableEvent> DurableStreamFrom(
            IQueryable<StorableEvent> events,
            long startAt)
        {
            events = events.OrderBy(e => e.Id);

            IEnumerator<StorableEvent> enumerator = null;
            var nextIdToFetch = startAt;

            Action reset = () =>
            {
                enumerator = events.Where(e => e.Id >= nextIdToFetch)
                                   .GetEnumerator();
            };

            reset();

            var receivedEvents = 0;

            while (true)
            {
                try
                {
                    if (appLockDisposer.IsDisposed)
                    {
                        yield break;
                    }

                    if (!enumerator.MoveNext())
                    {
                        if (receivedEvents >= expectedNumberOfEvents)
                        {
                            yield break;
                        }

                        reset();
                    }
                    else
                    {
                        receivedEvents++;
                    }
                }
                catch (InvalidOperationException exception)
                {
                    if (exception.Message.Contains("Invalid attempt to call IsDBNull when reader is closed.") ||
                        exception.Message.Contains("Calling 'Read' when the data reader is closed is not a valid operation."))
                    {
                        if (TryGetAppLock())
                        {
                            reset();
                            continue;
                        }
                    }
                    throw;
                }

                var @event = enumerator.Current;

                if (@event != null)
                {
                    nextIdToFetch = @event.Id + 1;

                    yield return @event;
                }
            }
        }

        public void Dispose() => appLockDisposer.Dispose();

        public override string ToString() => new  { lockResourceName, startAtId, events }.ToString();
    }
}
