// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    internal static class EventHandlerProgressCalculator
    {
        public static IEnumerable<EventHandlerProgress> CalculateProgress(
            Func<DbContext> createReadModelDbContext,
            Func<EventStoreDbContext> createEventStoreDbContext = null)
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
                readModelInfos = db.Set<ReadModelInfo>().ToArray();
            }

            readModelInfos
                .ForEach(i =>
                {
                    var eventsProcessed = i.InitialCatchupEndTime.HasValue
                                              ? EventsProcessedOutOfBatch(i)
                                              : EventsProcessedOutOfAllEvents(i);

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

                    var timeTakenForProcessedEvents = i.InitialCatchupEndTime.HasValue
                                                          ? (now - i.BatchStartTime).Value
                                                          : (now - i.InitialCatchupStartTime).Value;

                    var eventHandlerProgress = new EventHandlerProgress
                    {
                        Name = i.Name,
                        InitialCatchupEvents = i.InitialCatchupEvents,
                        TimeTakenForInitialCatchup = TimeTakenForInitialCatchup(i, now),
                        TimeRemainingForCatchup = TimeRemaining(timeTakenForProcessedEvents, eventsProcessed, i.BatchRemainingEvents),
                        EventsRemaining = i.BatchRemainingEvents,
                        PercentageCompleted = Percent(
                            eventStoreCount - i.BatchRemainingEvents,
                            eventStoreCount),
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

        private static long EventsProcessedOutOfAllEvents(ReadModelInfo i) => 
            i.InitialCatchupEvents - i.BatchRemainingEvents;

        private static long EventsProcessedOutOfBatch(ReadModelInfo i) => 
            i.BatchTotalEvents - i.BatchRemainingEvents;

        private static TimeSpan? TimeTakenForInitialCatchup(ReadModelInfo i, DateTimeOffset now)
        {
            if (i.InitialCatchupStartTime.HasValue)
            {
                return (i.InitialCatchupEndTime ?? now) - i.InitialCatchupStartTime;
            }

            return null;
        }

        private static TimeSpan? TimeRemaining(
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