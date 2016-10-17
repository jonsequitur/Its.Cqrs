// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An aggregate that can perform event migrations.
    /// </summary>
    public interface IEventMigratingAggregate
    {
         /// <summary>
        ///     Gets any rename operations for this aggregate that have not yet been committed to the event store.
        /// </summary>
        IEnumerable<EventMigrations.Rename> PendingRenames { get; }

        /// <summary>
        /// Confirms that a save operation has been successfully completed and that the aggregate should commit all pending rename operations to its event history.
        /// </summary>
        void ConfirmSave();
    }
}