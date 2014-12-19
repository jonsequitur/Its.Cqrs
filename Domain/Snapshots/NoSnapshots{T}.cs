// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public class NoSnapshots : ISnapshotRepository
    {
        public async Task<ISnapshot> Get(Guid aggregateId, long? maxVersion = null, DateTimeOffset? maxTimestamp = null)
        {
            return null;
        }

        public async Task SaveSnapshot(ISnapshot snapshot)
        {
        }
    }
}