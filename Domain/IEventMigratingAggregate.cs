// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    public interface IEventMigratingAggregate
    {
        IEnumerable<EventMigrations.Rename> PendingRenames { get; }

        void ConfirmSave();
    }
}