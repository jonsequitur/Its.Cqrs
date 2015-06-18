// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql
{
    public static class Sensors
    {
        private static ConcurrentDictionary<string, Func<DbContext>> _readModelDbContext;

        internal static ConcurrentDictionary<string, Func<DbContext>> ReadModelDbContexts
        {
            get { return _readModelDbContext ?? (_readModelDbContext = new ConcurrentDictionary<string, Func<DbContext>>()); }
        }

        public static Func<EventStoreDbContext> GetEventStoreDbContext = () => new EventStoreDbContext();

        [Export("DiagnosticSensor")]
        public static async Task<dynamic> CatchupStatus()
        {
            if (GetEventStoreDbContext() == null)
            {
                return "GetEventStoreDbContext on class Microsoft.Its.Domain.Sql.Sensors is not configured";
            }

            var latestEventId = Task.Run(() =>
            {
                using (var eventStore = GetEventStoreDbContext())
                {
                    return eventStore.Events
                                     .OrderByDescending(e => e.Id)
                                     .Select(e => e.Id)
                                     .FirstOrDefault();
                }
            });

            return new
            {
                LatestEventId = await latestEventId,
                ReadModels = ReadModelDbContexts.ToDictionary(p => p.Key,
                                                              p => EventHandlerProgressCalculator.Calculate(p.Value, GetEventStoreDbContext))
            };
        }
    }
}
