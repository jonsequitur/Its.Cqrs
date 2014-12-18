using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public interface ISnapshotRepository
    {
        Task<ISnapshot> Get(Guid aggregateId,
                            long? maxVersion = null,
                            DateTimeOffset? maxTimestamp = null);

        Task SaveSnapshot(ISnapshot snapshot);
    }
}