// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Allows a single reader among distributed instances to query and iterate events from the event store in order of their id, ascending.
    /// </summary>
    internal class ExclusiveEventStoreCatchupQuery : IDisposable
    {
        public int BatchSize { get; set; }
        private readonly EventStoreDbContext dbContext;
        private readonly string lockResourceName;
        private readonly SerialDisposable appLockDisposer = new SerialDisposable();
        private readonly IEnumerable<StorableEvent> events;
        private readonly long startAtId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveEventStoreCatchupQuery" /> class.
        /// </summary>
        /// <param name="dbContext">The event store database context to execute the query against.</param>
        /// <param name="lockResourceName">Name of the lock. Multiple instances compete with other instances having the same <paramref name="lockResourceName" />.</param>
        /// <param name="getStartAtId">The id of the first event to query.</param>
        /// <param name="applyFilter">Transforms a query to filter the events to be read from the event store.</param>
        /// <param name="batchSize">The number of events queried from the event store at each iteration.</param>
        public ExclusiveEventStoreCatchupQuery(
            EventStoreDbContext dbContext,
            string lockResourceName, 
            Func<long> getStartAtId, 
            Func< IQueryable<StorableEvent>, IQueryable<StorableEvent>> applyFilter,
            int batchSize = 10000)
        {
            if (batchSize < 1)
            {
                throw new ArgumentException($"{nameof(batchSize)} must be greater than zero.");
            }

            BatchSize = batchSize;
            this.dbContext = dbContext;
            this.lockResourceName = lockResourceName;

            if (TryGetAppLock())
            {
                startAtId = getStartAtId();

                IQueryable<StorableEvent> eventQuery =
                    applyFilter(dbContext.Events.AsNoTracking())
                        .Where(e => e.Id >= startAtId)
                        .OrderBy(e => e.Id);
                var oldCommandTimeout = dbContext.Database.CommandTimeout;
                try
                {
                    dbContext.Database.CommandTimeout = TimeSpan.FromMinutes(10).Seconds;
                    TotalMatchedEventCount = eventQuery.Count();
                }
                catch
                {
                    System.Diagnostics.Trace.WriteLine("Failed :eventQuery.Count() \n" + eventQuery.ToString());
                    throw;
                }
                finally
                {
                    dbContext.Database.CommandTimeout = oldCommandTimeout;
                }
                BatchMatchedEventCount = Math.Min(BatchSize, TotalMatchedEventCount);

                eventQuery = eventQuery.Take(batchSize);

                events = DurableStreamFrom(eventQuery, startAtId);
            }
            else
            {
                events = Enumerable.Empty<StorableEvent>();
            }
        }

        public virtual IEnumerable<StorableEvent> Events => events;

        public long BatchMatchedEventCount { get; }

        public long TotalMatchedEventCount { get; }

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
                        if (receivedEvents >= BatchMatchedEventCount)
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
