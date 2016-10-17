// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Represents information about the projection of a specific read model.
    /// </summary>
    public class ReadModelInfo
    {
        /// <summary>
        /// Gets or sets the name of the read model.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the time at which the read model was last updated.
        /// </summary>
        public DateTimeOffset? LastUpdated { get; set; }

        /// <summary>
        /// Gets the latest id processed by the projector.
        /// </summary>
        public long CurrentAsOfEventId { get; set; }

        /// <summary>
        /// Gets the last id on which the projector encountered an error.
        /// </summary>
        public long? FailedOnEventId { get; set; }

        /// <summary>
        /// Gets or sets the last error encountered while projecting this read model.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets the number of milliseconds between the writing of the most recent event handled by the projector and the time that that event was used to updated the read model.
        /// </summary>
        public double LatencyInMilliseconds { get; set; }

        /// <summary>
        /// Gets the time at which the projector began building the read model. This is typically the time at which the most recent read model database rebuild began.
        /// </summary>
        public DateTimeOffset? InitialCatchupStartTime { get; set; }

        /// <summary>
        /// Gets the number of events in the initial read model database rebuild.
        /// </summary>
        public long InitialCatchupEvents { get; set; }

        /// <summary>
        /// Gets the time at which the read model rebuild completed.
        /// </summary>
        public DateTimeOffset? InitialCatchupEndTime { get; set; }

        /// <summary>
        /// Gets the number of events remaining in the currently processing batch.
        /// </summary>
        public long BatchRemainingEvents { get; set; }

        /// <summary>
        /// Gets the time at which processing of the current batch of events began.
        /// </summary>
        public DateTimeOffset? BatchStartTime { get; set; }

        /// <summary>
        /// Gets the total number of events in the currently processing batch.
        /// </summary>
        public long BatchTotalEvents { get; set; }

        /// <summary>
        /// Gets the name for the specified projector.
        /// </summary>
        public static string NameForProjector(object projector) => EventHandler.FullName(projector);
    }
}