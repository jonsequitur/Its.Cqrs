// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive;

namespace Microsoft.Its.Domain
{
    public interface IEventBus
    {
        /// <summary>
        ///     Publishes the specified events.
        /// </summary>
        /// <param name="events">The events to be published.</param>
        /// <returns>
        ///     An <see cref="IObservable{T}" /> that will be notified once each time the event is handled.
        /// </returns>
        IObservable<Unit> PublishAsync(IEvent[] events);

        /// <summary>
        /// Publishes information about an error that arose during the handling of an event.
        /// </summary>
        /// <param name="error">The error</param>
        /// <returns>
        ///     An <see cref="IObservable{T}" /> that will be notified once each time the event is handled.
        /// </returns>
        IObservable<Unit> PublishErrorAsync(EventHandlingError error);

        /// <summary>
        ///     Gets an observable of all events of the specified type that are published on the bus.
        /// </summary>
        /// <typeparam name="T">The type of events to be observed.</typeparam>
        /// <returns>An observable sequence of events of the specified type.</returns>
        IObservable<T> Events<T>() where T : IEvent;

        /// <summary>
        /// Gets an observable sequence containing all errors that occur during handling of events published on the bus.
        /// </summary>
        IObservable<EventHandlingError> Errors { get;  }
    }
}