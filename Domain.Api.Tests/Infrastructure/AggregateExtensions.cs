// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;

namespace Microsoft.Its.Domain.Api.Tests.Infrastructure
{
    public static class AggregateExtensions
    {
        public static async Task SaveToEventStore<TAggregate>(this TAggregate aggregate) where TAggregate : EventSourcedAggregate
        {
            using (var db = new EventStoreDbContext())
            {
                foreach (var e in aggregate.EventHistory.OfType<IEvent<TAggregate>>())
                {
                    var storableEvent = e.ToStorableEvent();
                    db.Events.Add(storableEvent);
                }
                await db.SaveChangesAsync();
            }
        }
    }
}