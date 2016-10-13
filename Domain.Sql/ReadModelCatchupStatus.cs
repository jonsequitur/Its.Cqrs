// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides information about the status of a read model catchup.
    /// </summary>
    public struct ReadModelCatchupStatus
    {
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
        public bool IsEndOfBatch => BatchCount == NumberOfEventsProcessed;

        /// <summary>
        /// Gets a value indicating whether the status update indicates the start of a batch.
        /// </summary>
        public bool IsStartOfBatch => NumberOfEventsProcessed == 0;

        /// <summary>
        /// Gets or sets the time at which the related event was originally recorded.
        /// </summary>
        public DateTimeOffset? EventTimestamp { get; set; }

        /// <summary>
        /// Gets the latency between the time that the event was recorded and the time that it was projected.
        /// </summary>
        public TimeSpan? Latency
        {
            get
            {
                if (StatusTimeStamp != null && EventTimestamp != null)
                {
                    return StatusTimeStamp.Value - EventTimestamp.Value;
                }

                return null;
            }
        }

        internal DateTimeOffset? StatusTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the name of the catchup that send the status update.
        /// </summary>
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
                return
                    $"Catchup {CatchupName}: Processed {NumberOfEventsProcessed} of {BatchCount} (event id: {CurrentEventId} / recorded: {EventTimestamp} / latency: {Latency?.TotalSeconds}s)";
            }

            if (BatchCount == 0)
            {
                // CurrentEventId will be set to the next expected event id, so when there are no new events, subtracting 1 from it will be clearer
                return $"Catchup {CatchupName}: Found no new events after {CurrentEventId - 1}.";
            }

            return $"Catchup {CatchupName}: Starting from event {CurrentEventId} for {BatchCount} events";
        }
    }
}
