// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    internal class EventHandlerWrapper<TEvent> :
        IEventHandlerBinder,
        IUpdateProjectionWhen<TEvent>,
        IHaveConsequencesWhen<TEvent>,
        IEventHandlerWrapper
        where TEvent : IEvent
    {
        private Handle<TEvent> handle;
        
        public EventHandlerWrapper(IUpdateProjectionWhen<TEvent> projector)
        {
            if (projector == null)
            {
                throw new ArgumentNullException("projector");
            }
            InnerHandler = projector;
            IsConsequenter = false;
            handle = (@event, handler) => projector.UpdateProjection(@event);
        }

        public EventHandlerWrapper(IHaveConsequencesWhen<TEvent> consequenter)
        {
            if (consequenter == null)
            {
                throw new ArgumentNullException("consequenter");
            }
            InnerHandler = consequenter;
            IsConsequenter = true;
            handle = (@event, handler) => consequenter.HaveConsequences(@event);
        }

        public void Wrap(Handle<IEvent> first)
        {
            var next = handle;
            handle = (@event, handler) => first(@event, e => next((TEvent) e, null));
        }

        public Type EventType
        {
            get
            {
                return typeof (TEvent);
            }
        }

        public object InnerHandler { get; private set; }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            if (IsConsequenter)
            {
                return EventHandler.SubscribeConsequences(
                    this,
                    bus.Events<TEvent>(),
                    bus);
            }

            return EventHandler.SubscribeProjector(
                this,
                bus.Events<TEvent>(),
                bus);
        }

        public void UpdateProjection(TEvent @event)
        {
            handle(@event, e => { });
        }

        public void HaveConsequences(TEvent @event)
        {
            handle(@event, e => { });
        }

        protected bool IsConsequenter { get; private set; }
    }
}
