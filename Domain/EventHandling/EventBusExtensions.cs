// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides methods for working with the event bus.
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// Publishes events asynchronously on the specified event bus.
        /// </summary>
        /// <param name="bus">The event bus.</param>
        /// <param name="events">The events to be published.</param>
        /// <remarks>No events are actually published until the returned observable is subscribed.</remarks>
        public static IObservable<System.Reactive.Unit> PublishAsync(this IEventBus bus, params IEvent[] events) => 
            bus.PublishAsync(events);

        /// <summary>
        ///     Subscribes an event handler to events published on the bus.
        /// </summary>
        /// <param name="bus">The bus to whose events the handler will be subscribed.</param>
        /// <param name="handler">The handler to be subscribed to the bus.</param>
        /// <returns>
        ///     A disposable that can be disposed in order to cancel the subscription.
        /// </returns>
        public static IDisposable Subscribe(this IEventBus bus, object handler)
        {
            var inprocessBus = bus as InProcessEventBus;
            if (inprocessBus != null)
            {
                return inprocessBus.Subscribe(handler);
            }

            return new EventHandlerSubscription(handler, bus);
        }

        /// <summary>
        ///      Subscribes an event handler to events published on the bus.
        /// </summary>
        /// <param name="bus">The bus to whose events the handler will be subscribed.</param>
        /// <param name="handlers">The handlers to be subscribed to the bus.</param>
        /// <returns>
        ///     A disposable that can be disposed in order to cancel the subscriptions.
        /// </returns>
        public static IDisposable Subscribe(this IEventBus bus, params object[] handlers)
        {
            if (handlers == null || !handlers.Any())
            {
                return Disposable.Empty;
            }

            var disposable = new CompositeDisposable();
            handlers.ForEach(handler => disposable.Add(bus.Subscribe(handler)));
            return disposable;
        }
    }
}
