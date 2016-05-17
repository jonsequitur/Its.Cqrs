// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Creates snapshots from aggregate instances.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    public interface ICreateSnapshot<in TAggregate>
        where TAggregate : class, IEventSourced
    {
        /// <summary>
        /// Creates a snapshot from the specified aggregate.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        ISnapshot CreateSnapshot(TAggregate aggregate);
    }
}