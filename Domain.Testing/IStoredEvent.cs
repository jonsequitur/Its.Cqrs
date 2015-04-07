// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    ///     A sequential event applicable to a specific type, which can be used to rebuild the object's historical states.
    /// </summary>
    public interface IStoredEvent
    {
        /// <summary>
        /// Gets the position of the event within the source object's event sequence.
        /// </summary>
        long SequenceNumber { get; }

        // QUESTION: (IEvent) what's a better name for this, given that this event definition is not solely for event sourcing?
        string AggregateId { get; }

        /// <summary>
        /// Gets the time at which the event was originally recorded.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        string Type { get; set; }

        string Body { get; set; }

        string ETag { get; }
    }
}
