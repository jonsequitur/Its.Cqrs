using System;

namespace Microsoft.Its.Domain
{
    public static class Consequenter
    {
        public static IHaveConsequencesWhen<TEvent> Create<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent
        {
            return new AnonymousConsequenter<TEvent>(onEvent);
        }
    }
}