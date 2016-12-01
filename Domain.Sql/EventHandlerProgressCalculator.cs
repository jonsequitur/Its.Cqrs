// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain.Sql
{
    internal static class EventHandlerProgressCalculator
    {
        public static IEnumerable<EventHandlerProgress> CalculateProgress(
            Func<DbContext> createReadModelDbContext,
            Func<EventStoreDbContext> createEventStoreDbContext = null,
            Expression<Func<ReadModelInfo, bool>> filter = null)
        {
            if (createReadModelDbContext == null)
            {
                throw new ArgumentNullException(nameof(createReadModelDbContext));
            }

            createEventStoreDbContext = createEventStoreDbContext ??
                                        (() => Configuration.Current.EventStoreDbContext());

            int eventStoreCount;

            using (var db = createEventStoreDbContext())
            {
                eventStoreCount = db.Events.Count();
            }

            if (eventStoreCount == 0)
            {
                return Enumerable.Empty<EventHandlerProgress>();
            }

            var now = Clock.Now();
            var progress = new List<EventHandlerProgress>();

            ReadModelInfo[] readModelInfos;

            using (var db = createReadModelDbContext())
            {
                IQueryable<ReadModelInfo> query = db.Set<ReadModelInfo>();

                if (filter != null)
                {
                    query = query.Where(filter);
                }

                readModelInfos = query.ToArray();
            }

            readModelInfos
                .ForEach(i =>
                {
                    var isNotInitialCatchup = i.InitialCatchupEndTime.HasValue;

                    var eventsProcessed = EventsProcessedOutOfBatch(i);

                    if (eventsProcessed == 0)
                    {
                        return;
                    }

                    if (!i.BatchStartTime.HasValue)
                    {
                        return;
                    }

                    if (!i.InitialCatchupStartTime.HasValue)
                    {
                        return;
                    }

                    long eventsRemaining;
                    if (isNotInitialCatchup)
                    {
                        eventsRemaining = eventStoreCount - i.BatchRemainingEvents;
                    }
                    else
                    {
                        eventsRemaining = i.BatchTotalEvents - i.BatchRemainingEvents;
                    }

                    var timeTakenForProcessedEvents = isNotInitialCatchup
                                                          ? (now - i.BatchStartTime).Value
                                                          : (now - i.InitialCatchupStartTime).Value;

                    var percentageCompleted = Percent(
                        howMany: eventsRemaining,
                        outOf: eventStoreCount);

                    var timeRemainingForCatchup = TimeRemaining(
                        timeTakenForProcessedEvents,
                        eventsProcessed,
                        isNotInitialCatchup
                            ? i.BatchRemainingEvents
                            : eventStoreCount);

                    var eventHandlerProgress = new EventHandlerProgress
                    {
                        Name = i.Name,
                        InitialCatchupEvents = i.InitialCatchupEvents,
                        TimeTakenForInitialCatchup = TimeTakenForInitialCatchup(
                            i),
                        TimeRemainingForCatchup = timeRemainingForCatchup,
                        EventsRemainingInBatch = i.BatchRemainingEvents,
                        PercentageCompleted = percentageCompleted,
                        LatencyInMilliseconds = i.LatencyInMilliseconds,
                        LastUpdated = i.LastUpdated,
                        CurrentAsOfEventId = i.CurrentAsOfEventId,
                        FailedOnEventId = i.FailedOnEventId,
                        Error = i.Error
                    };

                    progress.Add(eventHandlerProgress);
                });

            return progress;
        }

        private static long EventsProcessedOutOfBatch(ReadModelInfo i) =>
            i.BatchTotalEvents - i.BatchRemainingEvents;

        private static TimeSpan? TimeTakenForInitialCatchup(ReadModelInfo i) =>
            i.InitialCatchupEndTime.HasValue
                ? i.InitialCatchupEndTime - i.InitialCatchupStartTime
                : null;

        private static TimeSpan TimeRemaining(
                TimeSpan timeTakenForProcessedEvents,
                long eventsProcessed,
                long eventsRemaining) =>
            TimeSpan.FromTicks((long) (timeTakenForProcessedEvents.Ticks*(eventsRemaining/(decimal) eventsProcessed)));

        internal static decimal Percent(decimal howMany, decimal outOf) =>
            outOf == 0
                ? 100
                : (howMany/outOf)*100;
    }
}