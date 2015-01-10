// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;

namespace Microsoft.Its.Domain
{
    internal class EventHandlerSubscription : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly IEventBus bus;

        internal EventHandlerSubscription(object handler, IEventBus bus)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            if (bus == null)
            {
                throw new ArgumentNullException("bus");
            }
            this.bus = bus;

            Subscribe(handler);
        }

        private void Subscribe(object handler)
        {
            EventHandler.GetBinders(handler)
                        .ForEach(m => disposables.Add(m.SubscribeToBus(handler, bus)));
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
