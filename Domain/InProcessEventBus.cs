// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// An in-process event bus.
    /// </summary>
    public class InProcessEventBus : IEventBus, IDisposable
    {
        private static readonly InProcessEventBus instance = new InProcessEventBus();
        private readonly CompositeDisposable disposables;
        private readonly ISubject<EventHandlingError> errorSubject;
        private readonly ISubject<IEvent> eventSubject;
        private readonly ConcurrentDictionary<object, IDisposable> handlersAndSubscriptions = new ConcurrentDictionary<object, IDisposable>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InProcessEventBus" /> class.
        /// </summary>
        /// <param name="eventSubject">The subject via which events are published.</param>
        /// <param name="errorSubject">The subject via which event handling errors are published.</param>
        public InProcessEventBus(
            ISubject<IEvent> eventSubject = null,
            ISubject<EventHandlingError> errorSubject = null)
        {
            disposables = new CompositeDisposable();

            if (eventSubject != null)
            {
                this.eventSubject = eventSubject;
            }
            else
            {
                var s = new Subject<IEvent>();
                disposables.Add(s);
                this.eventSubject = s;
            }

            if (errorSubject != null)
            {
                this.errorSubject = errorSubject;
            }
            else
            {
                var s = new Subject<EventHandlingError>();
                disposables.Add(s);
                this.errorSubject = s;
            }
        }

        /// <summary>
        ///     Publishes the specified events.
        /// </summary>
        /// <param name="events">The events to be published.</param>
        /// <returns>
        ///     An <see cref="IObservable{T}" /> that will be notified once and and then completed upon successful publication.
        /// </returns>
        public virtual IObservable<Unit> PublishAsync(IEvent[] events)
        {
            return Observable.Create<Unit>(observer =>
            {
                foreach (var e in events)
                {
                    eventSubject.OnNext(e);
                }

                observer.OnNext(Unit.Default);
                observer.OnCompleted();

                return Disposable.Empty;
            });
        }

        /// <summary>
        /// Publishes information about an error that arose during the handling of an event.
        /// </summary>
        /// <param name="error">The error</param>
        /// <returns>
        ///     An <see cref="IObservable{T}" /> that will be notified once each time the event is handled.
        /// </returns>
        public virtual IObservable<Unit> PublishErrorAsync(EventHandlingError error)
        {
            return Observable.Create<Unit>(observer =>
            {
                errorSubject.OnNext(error);
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return Disposable.Empty;
            });
        }

        /// <summary>
        ///     Gets an observable of all events of the specified type that are published on the bus.
        /// </summary>
        /// <typeparam name="T">The type of events to be observed.</typeparam>
        /// <returns></returns>
        public virtual IObservable<T> Events<T>() where T : IEvent
        {
            return eventSubject.OfType<T>();
        }

        /// <summary>
        /// Gets an observable sequence containing all errors that occur during handling of events published on the bus.
        /// </summary>
        public virtual IObservable<EventHandlingError> Errors
        {
            get
            {
                return errorSubject;
            }
        }

        /// <summary>
        ///     Subscribes an event handler to events published on the bus.
        /// </summary>
        internal virtual IDisposable Subscribe(object handler)
        {
            return handlersAndSubscriptions.GetOrAdd(handler, h =>
            {
                var subscription = new EventHandlerSubscription(h, this);
                disposables.Add(subscription);
                return subscription;
            });
        }

        /// <summary>
        /// A single global instance of <see cref="InProcessEventBus" />.
        /// </summary>
        public static InProcessEventBus Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public virtual void Dispose()
        {
            disposables.Dispose();
        }
    }
}