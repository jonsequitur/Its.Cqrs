using System;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for working with <see cref="EventStoreDbContext"/>.
    /// </summary>
    public static class EventStoreDbContextExtensions
    {
        /// <summary>
        /// Returns the Id of the latest event written to the event store, or 0 if the event store is empty. 
        /// </summary>
        public static long HighestEventId(this EventStoreDbContext db) =>
            db.Events.Max<StorableEvent, long?>(e => e.Id) ?? 0;
    }
}
