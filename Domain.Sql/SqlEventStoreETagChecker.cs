// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Verifies whether an etag has been recorded in a SQL-backed event store.
    /// </summary>
    public class SqlEventStoreETagChecker : IETagChecker
    {
        private readonly Func<EventStoreDbContext> createEventStoreDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlEventStoreETagChecker"/> class.
        /// </summary>
        /// <param name="createEventStoreDbContext">A delegate to create <see cref="EventStoreDbContext" /> instances.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public SqlEventStoreETagChecker(Func<EventStoreDbContext> createEventStoreDbContext)
        {
            if (createEventStoreDbContext == null)
            {
                throw new ArgumentNullException(nameof(createEventStoreDbContext));
            }
            this.createEventStoreDbContext = createEventStoreDbContext;
        }

        /// <summary>
        /// Determines whether the specified etag has been recorded within the specified scope.
        /// </summary>
        /// <param name="scope">The scope within which the etag is unique.</param>
        /// <param name="etag">The etag.</param>
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