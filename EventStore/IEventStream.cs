using System;
using System.Collections.Generic;

namespace Microsoft.Its.EventStore
{
    public interface IEventStream
    {
        // QUESTION: (IEventStream) is there a better param name than aggregateId for these methods?

        /// <summary>
        /// Appends an event to the stream.
        /// </summary>
        /// <param name="e">The event to append to the stream.</param>
        void Append(IStoredEvent e);

        /// <summary>
        /// Gets the latest event in the specified event stream with the specified aggregate id.
        /// </summary>
        /// <returns></returns>
        IStoredEvent Latest(string aggregateId);

        /// <summary>
        /// Gets all of the events in the stream with the specified aggregate id.
        /// </summary>
        IEnumerable<IStoredEvent> All(string aggregateId);

        /// <summary>
        /// Gets all of the events in the stream created with the specified aggregate id as of the specified date.
        /// </summary>
        IEnumerable<IStoredEvent> AsOfDate(string aggregateId, DateTimeOffset date);

        /// <summary>
        /// Gets all of the events in the stream created with the specified aggregate id up to (and including) the specified version.
        /// </summary>
        IEnumerable<IStoredEvent> UpToVersion(string aggregateId, long version);
    }
}