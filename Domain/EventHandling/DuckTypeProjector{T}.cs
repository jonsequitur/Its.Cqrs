using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Unit = System.Reactive.Unit;

namespace Microsoft.Its.Domain
{
    internal class DuckTypeProjector<T> :
        IUpdateProjectionWhen<IEvent>,
        IEventHandler,
        IEventHandlerBinder,
        IEventQuery
    {
        private readonly Action<T> onEvent;
        private static readonly string streamName;
        private static readonly string eventName;
        private readonly ConcurrentDictionary<Type, Unit> eventTypesWithNoStreamNameSetter = new ConcurrentDictionary<Type, Unit>();

        static DuckTypeProjector()
        {
            eventName = typeof (T).Name;

            if (eventName == "IEvent" || eventName == "Event" || eventName == "Object")
            {
                eventName = MatchEvent.Wildcard;
            }

            streamName = typeof (T).IsNested ? AggregateType.EventStreamName(typeof (T).DeclaringType) : "";
        }

        public DuckTypeProjector(Action<T> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException("onEvent");
            }
            this.onEvent = onEvent;
        }

        public void UpdateProjection(IEvent @event)
        {
            var json = @event.ToJson();
            
            dynamic duckTypedEvent;
            
            if (typeof(T).IsAbstract)
            {
                duckTypedEvent = new DynamicEvent(json.FromJsonTo<dynamic>());
            }
            else
            {
                duckTypedEvent = json.FromJsonTo<T>();
            }
            
            TrySetStreamNameFrom(@event.GetType(), duckTypedEvent, @event.EventStreamName());

            onEvent(duckTypedEvent);
        }

        private void TrySetStreamNameFrom(Type eventType, dynamic duckTypedEvent, string eventStreamName)
        {
            if (!eventTypesWithNoStreamNameSetter.ContainsKey(eventType))
            {
                try
                {
                    duckTypedEvent.EventStreamName = eventStreamName;
                }
                catch (RuntimeBinderException)
                {
                    eventTypesWithNoStreamNameSetter.GetOrAdd(eventType, _ => Unit.Default);
                }
            }
        }

        public IEnumerable<MatchEvent> IncludedEventTypes
        {
            get
            {
                return new[]
                {
                    new MatchEvent(typeof (T).Name,
                                   streamName:
                                       typeof (T).AggregateTypeForEventType()
                                                 .IfNotNull()
                                                 .Then(AggregateType.EventStreamName)
                                                 .Else(() => "*"))
                };
            }
        }

        public IEnumerable<IEventHandlerBinder> GetBinders()
        {
            return new IEventHandlerBinder[] { this };
        }

        public Type EventType
        {
            get
            {
                return typeof (IEvent);
            }
        }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            Func<IEvent, bool> predicate;
            if (string.IsNullOrWhiteSpace(streamName))
            {
                predicate = e => e.EventName() == eventName || eventName == MatchEvent.Wildcard;
            }
            else
            {
                predicate = e => (e.EventName() == eventName || eventName == MatchEvent.Wildcard) &&
                                 (e.EventStreamName() == streamName || streamName == MatchEvent.Wildcard);
            }

            return bus.Events<IEvent>()
                      .Where(predicate)
                      .SubscribeDurablyAndPublishErrors(this, UpdateProjection, bus);
        }
    }
}