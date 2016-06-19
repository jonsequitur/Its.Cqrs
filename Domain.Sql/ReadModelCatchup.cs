// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;

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
        /// <param name="projectors">The projectors to be updated as new events are added to the event store.</param>
        /// <exception cref="System.ArgumentException">You must specify at least one projector.</exception>
        public ReadModelCatchup(
            Func<DbContext> readModelDbContext,
            Func<EventStoreDbContext> eventStoreDbContext,
            long startAtEventId = 0,
            params object[] projectors) :
                base(readModelDbContext,
                     eventStoreDbContext,
                     startAtEventId,
                     projectors)
        {
        }
    }
}