using System;

namespace Microsoft.Its.Domain
{
    internal class AnonymousProjector<TEvent> : IUpdateProjectionWhen<TEvent>, INamedEventHandler
        where TEvent : IEvent
    {
        private readonly Action<TEvent> onEvent;

        public AnonymousProjector(Action<TEvent> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException("onEvent");
            }
            this.onEvent = onEvent;
        }

        public void UpdateProjection(TEvent @event)
        {
            onEvent(@event);
        }

        public string Name { get; set; }
    }
}