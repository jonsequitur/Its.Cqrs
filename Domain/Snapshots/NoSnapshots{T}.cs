// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class NoSnapshots : ISnapshotRepository
    {
        public Task<ISnapshot> GetSnapshot(Guid aggregateId, long? maxVersion = null, DateTimeOffset? asOfDate = null) =>
            Task.FromResult<ISnapshot>(null);

        public Task SaveSnapshot(ISnapshot snapshot) => Task.FromResult(Unit.Default);
    }
}