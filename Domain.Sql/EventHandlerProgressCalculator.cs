// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Transactions;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql
{
    internal static class EventHandlerProgressCalculator
    {
        public static IEnumerable<EventHandlerProgress> Calculate(
            Func<DbContext> createDbContext,
            Func<EventStoreDbContext> createEventStoreDbContext = null)
        {
            if (createDbContext == null)
            {
                throw new ArgumentNullException("createDbContext");
            }

            int count;

            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var db = createEventStoreDbContext.IfNotNull()
                                                     .Then(create => create())
                                                     .Else(() => new EventStoreDbContext()))
            {
                count = db.Events.Count();
            }

            var now = Clock.Now();
            var progress = new List<EventHandlerProgress>();

            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var db = createDbContext())
            {
                db.Set<ReadModelInfo>()
                  .ForEach(i =>
                  {
                      var eventsProcessed = i.InitialCatchupEndTime.HasValue
                                                ? i.BatchTotalEvents - i.BatchRemainingEvents
                                                : i.InitialCatchupEvents - i.BatchRemainingEvents;

                      long? timeTakenForProcessedEvents = null;
                      if (i.BatchStartTime.HasValue && i.InitialCatchupStartTime.HasValue)
                      {
                          timeTakenForProcessedEvents = i.InitialCatchupEndTime.HasValue
                                                            ? (now - i.BatchStartTime).Value.Ticks
                                                            : (now - i.InitialCatchupStartTime).Value.Ticks;
                      }

                      progress.Add(new EventHandlerProgress
                      {
                          Name = i.Name,
                          InitialCatchupEvents = i.InitialCatchupEvents,
                          TimeTakenForInitialCatchup = i.InitialCatchupStartTime.HasValue
                                                           ? (i.InitialCatchupEndTime.HasValue ? i.InitialCatchupEndTime : now) - i.InitialCatchupStartTime
                                                           : null,
                          TimeRemainingForCatchup = eventsProcessed != 0 && timeTakenForProcessedEvents.HasValue
                                                        ? (TimeSpan?) TimeSpan.FromTicks((long) (timeTakenForProcessedEvents*(i.BatchRemainingEvents/(decimal) eventsProcessed)))
                                                        : null,
                          EventsRemaining = i.BatchRemainingEvents,
                          PercentageCompleted = (1 - ((decimal) i.BatchRemainingEvents/count))*100,
                          LatencyInMilliseconds = i.LatencyInMilliseconds,
                          LastUpdated = i.LastUpdated,
                          CurrentAsOfEventId = i.CurrentAsOfEventId,
                          FailedOnEventId = i.FailedOnEventId,
                          Error = i.Error
                      });
                  });
            }

            return progress;
        }
    }
}