using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public class NoSnapshots : ISnapshotRepository
    {
        public async Task<ISnapshot> Get(Guid aggregateId, long? maxVersion = null, DateTimeOffset? maxTimestamp = null)
        {
            return null;
        }

        public async Task SaveSnapshot(ISnapshot snapshot)
        {
        }
    }
}