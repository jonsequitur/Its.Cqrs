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
    public interface IEventSourced
    {
        /// <summary>
        ///     Gets the globally unique id for this aggregate.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the highest sequence number in the source event stream.
        /// </summary>
        long Version { get; }

        /// <summary>
        ///     Gets any events for this aggregate that have not yet been committed to the event store.
        /// </summary>
        IEnumerable<IEvent> PendingEvents { get; }

        /// <summary>
        /// Confirms that a save operation has been successfully completed and that the aggregate should move all pending events to its event history.
        /// </summary>
        void ConfirmSave();
    }
}