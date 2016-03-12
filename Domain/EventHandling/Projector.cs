// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class Projector
    {
        public static IUpdateProjectionWhen<TEvent> Create<TEvent>(Action<TEvent> onEvent) where TEvent : IEvent =>
            new AnonymousProjector<TEvent>(onEvent);

        public static IUpdateProjectionWhen<IEvent> CreateFor<T>(Action<T> onEvent) =>
            new DuckTypeProjector<T>(onEvent);

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

        public static IEventHandler Combine(params object[] projectors) =>
            new CompositeEventHandler(projectors);
    }
}