// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Handles an event, including calling the next handler in a chain.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="nextHandler">The next handler.</param>
    public delegate void Handle<TEvent>(TEvent @event, Action<TEvent> nextHandler) where TEvent : IEvent;
}
