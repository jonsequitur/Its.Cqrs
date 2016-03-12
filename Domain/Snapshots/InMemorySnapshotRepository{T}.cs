// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public class InMemorySnapshotRepository : ISnapshotRepository
    {
        private readonly ConcurrentDictionary<Guid, ISnapshot> snapshots = new ConcurrentDictionary<Guid, ISnapshot>();

        public async Task<ISnapshot> GetSnapshot(Guid aggregateId, long? maxVersion = null, DateTimeOffset? maxTimestamp = null) => 
            snapshots.IfContains(aggregateId).ElseDefault();

        public async Task SaveSnapshot(ISnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            snapshots[snapshot.AggregateId] = snapshot;
        }
    }
}