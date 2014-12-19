// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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