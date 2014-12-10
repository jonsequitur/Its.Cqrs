using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides information about the status of a read model catchup.
    /// </summary>
    public struct ReadModelCatchupStatus
    {
        private DateTimeOffset? eventTimestamp;

        /// <summary>
        /// Gets or sets the count of events in the current batch.
        /// </summary>
        public long BatchCount { get; set; }

        /// <summary>
        /// Gets or sets the event identifier of the event currently being projected.
        /// </summary>
        public long CurrentEventId { get; set; }

        /// <summary>
        /// Gets or sets the number of events processed in the current batch.
        /// </summary>
        public long NumberOfEventsProcessed { get; set; }

        /// <summary>
        /// Gets a value indicating whether this status update represents the last event in a batch.
        /// </summary>
        public bool IsEndOfBatch
        {
            get
            {
                return BatchCount == NumberOfEventsProcessed;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the status update indicates the start of a batch.
        /// </summary>
        public bool IsStartOfBatch
        {
            get
            {
                return NumberOfEventsProcessed == 0;
            }
        }

        public DateTimeOffset? EventTimestamp   
        {
            get
            {
                return eventTimestamp;
            }
            set
            {
                eventTimestamp = value;
            }
        }
        
        public TimeSpan? Latency
        {
            get
            {
                if (StatusTimeStamp != null && eventTimestamp != null)
                {
                    return StatusTimeStamp.Value - eventTimestamp.Value;
                }

                return null;
            }
        }

        internal DateTimeOffset? StatusTimeStamp { get; set; }

        public string CatchupName { get; set; }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> containing a fully qualified type name.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            if (NumberOfEventsProcessed > 0)
            {
                return string.Format("Catchup {0}: Processed {1} of {2} (event id: {3} / recorded: {4} / latency: {5}s)",
                                     CatchupName,
                                     NumberOfEventsProcessed,
                                     BatchCount,
                                     CurrentEventId,
                                     EventTimestamp,
                                     (Latency.Value).TotalSeconds);
            }

            if (BatchCount == 0)
            {
                // CurrentEventId will be set to the next expected event id, so when there are no new events, subtracting 1 from it will be clearer
                return string.Format("Catchup {0}: Found no new events after {1}.",
                                     CatchupName,
                                     CurrentEventId - 1);
            }

            return string.Format("Catchup {0}: Starting from event {1} for {2} events",
                                 CatchupName,
                                 CurrentEventId,
                                 BatchCount);
        }
    }
}