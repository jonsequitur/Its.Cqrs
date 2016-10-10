// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
       /// <summary>
    /// Saves and retrieves snapshots of aggregates in memory.
    /// </summary>
    public class InMemorySnapshotRepository : ISnapshotRepository
    {
        private readonly ConcurrentDictionary<Guid, ISnapshot> snapshots = new ConcurrentDictionary<Guid, ISnapshot>();

        /// <summary>
        /// Gets the snapshot for the specified aggregate.
        /// </summary>
        /// <remarks>By default, this gets the most recent snapshot (by version number) but older versions can be accessed by passing maxVersion or maxTimestamp.</remarks>
        public Task<ISnapshot> GetSnapshot(Guid aggregateId, long? maxVersion = null, DateTimeOffset? maxTimestamp = null) => 
            Task.FromResult(snapshots.IfContains(aggregateId).ElseDefault());

        /// <summary>
        /// Saves a snapshot.
        /// </summary>
        public Task SaveSnapshot(ISnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            snapshots[snapshot.AggregateId] = snapshot;

            return Task.FromResult(Unit.Default);
        }
    }
}