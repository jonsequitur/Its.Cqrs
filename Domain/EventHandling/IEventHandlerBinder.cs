// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Binds events published on the bus to specific methods on the target handler.
    /// </summary>
    public interface IEventHandlerBinder
    {
        /// <summary>
        /// Subscribes the specified handler to the event bus.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="bus">The bus.</param>
        IDisposable SubscribeToBus(object handler, IEventBus bus);
    }
}
