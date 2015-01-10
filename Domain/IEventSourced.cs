// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     Represents an aggregate whose state can be persisted as a sequence of events.
    /// </summary>
    public interface IEventSourced : IAggregateRoot
    {
        long Version { get; }

        /// <summary>
        ///     Gets any events for this aggregate that have not yet been committed to the event store.
        /// </summary>
        IEnumerable<IEvent> PendingEvents { get; }
    }
}
