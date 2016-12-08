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
                    var eventsProcessedOutOfBatch = EventsProcessedOutOfBatch(i);

                    if (eventsProcessedOutOfBatch == 0)
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

                    var eventHandlerProgress = new EventHandlerProgress(i);

                    progress.Add(eventHandlerProgress);
                });

            return progress;
        }

        private static long EventsProcessedOutOfBatch(ReadModelInfo i) =>
            i.BatchTotalEvents - i.BatchRemainingEvents;
    }
}