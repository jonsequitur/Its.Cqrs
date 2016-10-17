// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An aggregate snapshot.
    /// </summary>
    public interface ISnapshot
    {
        /// <summary>
        /// Gets the id of the aggregate that this is a snapshot of.
        /// </summary>
        Guid AggregateId { get; set; }

        /// <summary>
        /// Gets or sets the version at which the aggregate was snapshotted.
        /// </summary>
        long Version { get; set; }

        /// <summary>
        /// Gets or sets the time at which the snapshot was last updated.
        /// </summary>
        DateTimeOffset LastUpdated { get; set; }

        /// <summary>
        /// Gets or sets the name of the aggregate type.
        /// </summary>
        string AggregateTypeName { get; set; }

        /// <summary>
        /// Gets or sets a Bloom filter representing the etags for all of the snapshotted aggregate's events.
        /// </summary>
        BloomFilter ETags { get; set; }

        /// <summary>
        /// Gets or sets the serialized snapshot.
        /// </summary>
        string Body { get; set; }
    }
}