// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Triggers a business process upon receiving an event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <remarks>Typically these are actions that should not be repeated, e.g. charging for an order.</remarks>
    [Obsolete("Support will be removed in a forthcoming version. Please use the command scheduler instead.")]
    public interface IHaveConsequencesWhen<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// Triggers consequences of the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        void HaveConsequences(TEvent @event);
    }
}
