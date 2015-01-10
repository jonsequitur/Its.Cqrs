// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    internal class ReflectedEventHandlerBinder : IEventHandlerBinder
    {
        private readonly bool isConsequenter;

        public ReflectedEventHandlerBinder(Type eventType, Type handlerInterface)
        {
            EventType = eventType;
            HandlerInterface = handlerInterface;
            isConsequenter = HandlerInterface.GetGenericTypeDefinition() == typeof (IHaveConsequencesWhen<>);
        }

        public Type HandlerInterface { get; private set; }

        public IObservable<IEvent> GetEventsObservableFromBus(IEventBus bus)
        {
            var events = bus.GetType().GetMethod("Events").MakeGenericMethod(EventType);
            return (IObservable<IEvent>) events.Invoke(bus, null);
        }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            if (isConsequenter)
            {
                return EventHandler.SubscribeConsequences((dynamic) handler,
                                                          (dynamic) GetEventsObservableFromBus(bus),
                                                          bus);
            }

            return EventHandler.SubscribeProjector((dynamic) handler,
                                                   (dynamic) GetEventsObservableFromBus(bus),
                                                   bus);
        }

        public Type EventType { get; private set; }
    }
}
