// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Saves and retrieves snapshots of aggregates.
    /// </summary>
    public interface ISnapshotRepository
    {
        /// <summary>
        /// Gets the snapshot for the specified aggregate.
        /// </summary>
        /// <remarks>By default, this gets the most recent snapshot (by version number) but older versions can be accessed by passing maxVersion or maxTimestamp.</remarks>
        Task<ISnapshot> GetSnapshot(Guid aggregateId,
                            long? maxVersion = null,
                            DateTimeOffset? maxTimestamp = null);

        /// <summary>
        /// Saves a snapshot.
        /// </summary>
        Task SaveSnapshot(ISnapshot snapshot);
    }
}