// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Handles events published on an <see cref="IEventBus" />.
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Gets the binders for the handler.
        /// </summary>
        IEnumerable<IEventHandlerBinder> GetBinders();
    }
}
