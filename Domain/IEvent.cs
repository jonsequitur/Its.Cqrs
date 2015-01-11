// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A sequential event applicable to a specific type, which can be used to rebuild the object's historical states.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Gets the position of the event within the source object's event sequence.
        /// </summary>
        long SequenceNumber { get; }

        /// <summary>
        /// Gets the unique id of the source object to which this event applies.
        /// </summary>
        Guid AggregateId { get; }

        /// <summary>
        /// Gets the time at which the event was originally recorded.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the event's ETag, which is used to support idempotency within the event stream.
        /// </summary>
        string ETag { get; }
    }
}
