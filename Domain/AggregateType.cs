// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class AggregateType
    {
        private static readonly IDictionary<Type, string> knownTypes =
            Discover.ConcreteTypesDerivedFrom(typeof (IEventSourced))
                    .ToDictionary(t => t,
                                  t => typeof (AggregateType<>).MakeGenericType(t).Member().EventStreamName as string);

        public static Type[] KnownTypes
        {
            get
            {
                return knownTypes.Keys.ToArray();
            }
        }

        public static string EventStreamName(this IEvent @event)
        {
            return @event.IfTypeIs<DynamicEvent>()
                         .Then(e => e.EventStreamName)
                         .Else(() => EventStreamName(@event.AggregateType()));
        }

        public static string EventStreamName(Type aggregateType)
        {
            string value;
            if (knownTypes.TryGetValue(aggregateType, out value))
            {
                return value;
            }

            return aggregateType.Name;
        }
    }
}
