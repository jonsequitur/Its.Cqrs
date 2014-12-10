using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Microsoft.Its.Domain.Tests.Infrastructure
{
    public class NoOpEventBus : IEventBus
    {
        public static readonly NoOpEventBus Instance = new NoOpEventBus();

        public IObservable<Unit> PublishAsync(params IEvent[] events)
        {
            return Observable.Return(Unit.Default);
        }

        public IObservable<Unit> PublishErrorAsync(EventHandlingError error)
        {
            return Observable.Return(Unit.Default);
        }

        public IObservable<T> Events<T>() where T : IEvent
        {
            return Observable.Never<T>();
        }

        public IObservable<EventHandlingError> Errors
        {
            get
            {
                return Observable.Never<EventHandlingError>();
            }
        }
    }
}