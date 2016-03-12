// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
#pragma warning disable 618
    internal class AnonymousConsequenter<TEvent> : IHaveConsequencesWhen<TEvent>, INamedEventHandler
#pragma warning restore 618
        where TEvent : IEvent
    {
        private readonly Action<TEvent> onEvent;

        public AnonymousConsequenter(Action<TEvent> onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }
            this.onEvent = onEvent;
        }

        public void HaveConsequences(TEvent @event) => onEvent(@event);

        public string Name { get; set; }
    }
}
