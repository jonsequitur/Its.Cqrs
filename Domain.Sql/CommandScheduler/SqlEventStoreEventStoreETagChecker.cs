// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class SqlEventStoreEventStoreETagChecker : IETagChecker
    {
        private readonly Func<EventStoreDbContext> createEventStoreDbContext;

        public SqlEventStoreEventStoreETagChecker(Func<EventStoreDbContext> createEventStoreDbContext = null)
        {
            this.createEventStoreDbContext = createEventStoreDbContext ??
                                             (() => new EventStoreDbContext());
        }

        public async Task<bool> HasBeenRecorded(string scope, string etag)
        {
            var aggregateId = Guid.Parse(scope);

            using (var eventStore = createEventStoreDbContext())
            {
                return await eventStore.Events
                                       .Where(e => e.AggregateId == aggregateId &&
                                                   e.ETag == etag).AnyAsync();
            }
        }
    }
}