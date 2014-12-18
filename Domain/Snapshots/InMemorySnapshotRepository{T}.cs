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

        public async Task<ISnapshot> Get(Guid aggregateId, long? maxVersion = null, DateTimeOffset? maxTimestamp = null)
        {
            return snapshots.IfContains(aggregateId).ElseDefault();
        }

        public async Task SaveSnapshot(ISnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }

            snapshots[snapshot.AggregateId] = snapshot;
        }
    }
}