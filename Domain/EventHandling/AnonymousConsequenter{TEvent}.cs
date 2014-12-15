// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    internal class AnonymousConsequenter<TEvent> : IHaveConsequencesWhen<TEvent>, INamedEventHandler
        where TEvent : IEvent
    {
        private readonly Action<TEvent> onEvent;

        public AnonymousConsequenter(Action<TEvent> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException("onEvent");
            }
            this.onEvent = onEvent;
        }

        public void HaveConsequences(TEvent @event)
        {
            onEvent(@event);
        }

        public string Name { get; set; }
    }
}