// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Updates read models using <see cref="ReadModelDbContext" /> based on events after they have been added to an event store.
    /// </summary>
    public class ReadModelCatchup : ReadModelCatchup<ReadModelDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelCatchup{TDbContext}" /> class.
        /// </summary>
        /// <param name="readModelDbContext">A delegate to create read model database contexts on demand.</param>
        /// <param name="eventStoreDbContext">A delegate to create event store database contexts on demand.</param>
        /// <param name="startAtEventId">The event id that the catchup should start from.</param>
        /// <param name="batchSize">The number of events queried from the event store at each iteration.</param>
        /// <param name="filter">An optional filter expression to constrain the query that the catchup uses over the event store.</param>
        /// <param name="projectors">The projectors to be updated as new events are added to the event store.</param>
        /// <exception cref="System.ArgumentException">You must specify at least one projector.</exception>
        public ReadModelCatchup(
            Func<DbContext> readModelDbContext,
            Func<EventStoreDbContext> eventStoreDbContext,
            long startAtEventId = 0,
            int batchSize = 10000,
            Expression<Func<StorableEvent, bool>> filter = null,
            params object[] projectors) :
            base(readModelDbContext,
                eventStoreDbContext,
                startAtEventId,
                batchSize,
                filter,
                projectors)
        {
        }
    }
}