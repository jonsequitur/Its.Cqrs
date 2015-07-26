// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An optional interface for Repositories which are capable of "migration" activities.
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public interface IMigratableEventSourcedRepository<in TAggregate>
        where TAggregate : class, IEventSourced
    {
        /// <summary>
        /// A variation of <see cref="IEventSourcedRepository{TAggregate}.Save"/> that renames existing events in the same transaction."/>
        /// </summary>
        Task SaveWithRenames(TAggregate aggregate, IEnumerable<EventMigrator.Rename> pendingRenames);
    }
}