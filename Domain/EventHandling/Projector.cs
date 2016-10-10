// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Methods for creating anonymous projectors.
    /// </summary>
    public static class Projector
    {
        /// <summary>
        /// Creates an anonymous projector.
        /// </summary>
        public static IUpdateProjectionWhen<TEvent> Create<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent =>
            new AnonymousProjector<TEvent>(onEvent);

        /// <summary>
        /// Creates a duck-typed projector.
        /// </summary>
        public static IUpdateProjectionWhen<IEvent> CreateFor<T>(Action<T> onEvent) =>
            new DuckTypeProjector<T>(onEvent);

        /// <summary>
        /// Creates a dynamic projector.
        /// </summary>
        /// <param name="onEvent">A delegate that's called when an event is handled.</param>
        /// <param name="eventTypes">The event types that the projector handles.</param>
        public static IEventHandler CreateDynamic(
            Action<dynamic> onEvent,
            params string[] eventTypes)
        {
            var matchEvents = eventTypes.OrEmpty().Select(e => e.Split(new[]
            {
                '.'
            }, StringSplitOptions.RemoveEmptyEntries))
                                        .Select(e =>
                                        {
                                            if (e.Length == 1)
                                            {
                                                // just the event type
                                                return new MatchEvent(type: e[0]);
                                            }

                                            // AggregateType.EventType
                                            return new MatchEvent(streamName: e[0], type: e[1]);
                                        }).ToArray();

            return new DynamicProjector(onEvent, matchEvents);
        }

        /// <summary>
        /// Creates a composite projector containing the specified projectors.
        /// </summary>
        /// <param name="projectors">The projectors to be combined.</param>
        public static IEventHandler Combine(params object[] projectors) =>
            new CompositeEventHandler(projectors);
    }
}