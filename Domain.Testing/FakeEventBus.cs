// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Provides an implementation for testing IEventBus interactions.
    /// </summary>
    public class FakeEventBus : InProcessEventBus
    {
        // TODO: (FakeEventBus) rename this
        private readonly List<IEvent> publishedEvents = new List<IEvent>();
        private readonly List<Type> subscribedEventTypes = new List<Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeEventBus"/> class.
        /// </summary>
        public FakeEventBus() : base(new Subject<IEvent>(), new Subject<EventHandlingError>())
        {
        }

        /// <summary>
        /// Gets or sets the scheduler on which the bus schedules work.
        /// </summary>
        public IScheduler Scheduler { get; set; } = System.Reactive.Concurrency.Scheduler.Immediate;

        /// <summary>
        ///     Publishes the specified events.
        /// </summary>
        /// <param name="events">The events to be published.</param>
        /// <returns>
        ///     An <see cref="IObservable{T}" /> that will be notified once each time the event is handled.
        /// </returns>
        public override IObservable<Unit> PublishAsync(IEvent[] events)
        {
            lock (publishedEvents)
            {
                foreach (var @event in events)
                {
                    publishedEvents.Add(@event);
                }
            }

            return base.PublishAsync(events);
        }

        /// <summary>
        /// Publishes information about an error that arose during the handling of an event.
        /// </summary>
        /// <param name="error">The error</param>
        /// <returns>
        ///     An <see cref="IObservable{T}" /> that will be notified once each time the event is handled.
        /// </returns>
        public override IObservable<Unit> PublishErrorAsync(EventHandlingError error)
        {
            return base.PublishErrorAsync(error)
                       .ObserveOn(Scheduler)
                       .SubscribeOn(Scheduler);
        }

        /// <summary>
        ///     Gets an observable of all events of the specified type that are published on the bus.
        /// </summary>
        /// <typeparam name="T">The type of events to be observed.</typeparam>
        /// <returns>An observable sequence of events of the specified type.</returns>
        public override IObservable<T> Events<T>()
        {
            lock (subscribedEventTypes)
            {
                subscribedEventTypes.Add(typeof (T));
            }

            return base.Events<T>()
                       .ObserveOn(Scheduler)
                       .SubscribeOn(Scheduler);
        }

        /// <summary>
        /// Gets a sequence of all of the events that have been published on this bus instance.
        /// </summary>
        public IEnumerable<IEvent> PublishedEvents()
        {
            lock (publishedEvents)
            {
                return publishedEvents.ToArray();
            }
        }

        /// <summary>
        /// Gets a sequence of all of the event types that have been subscribed on this bus.
        /// </summary>
        public IEnumerable<Type> SubscribedEventTypes()
        {
            lock (subscribedEventTypes)
            {
                return subscribedEventTypes.ToArray();
            }
        }

        /// <summary>
        /// Clears the <see cref="PublishedEvents" /> and <see cref="SubscribedEventTypes" /> lists.
        /// </summary>
        public void Clear()
        {
            lock (subscribedEventTypes)
            {
                subscribedEventTypes.Clear();
            }
            lock (publishedEvents)
            {
                publishedEvents.Clear();
            }
        }
    }
}
