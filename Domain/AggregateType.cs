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
    /// <summary>
    /// Provides information about aggregate types in the current AppDomain.
    /// </summary>
    public static class AggregateType
    {
        private static readonly IDictionary<Type, string> knownTypes =
            Discover.ConcreteTypesDerivedFrom(typeof (IEventSourced))
                    .ToDictionary(t => t,
                                  t => typeof (AggregateType<>).MakeGenericType(t).Member().EventStreamName as string);

        /// <summary>
        /// Gets the types derived from <see cref="IEventSourced" /> discovered in the current <see cref="AppDomain" />.
        /// </summary>
        public static Type[] KnownTypes => knownTypes.Keys.ToArray();

        /// <summary>
        /// Gets the name of the event stream (e.g. the aggregate type name) to which the specified event belongs.
        /// </summary>
        public static string EventStreamName(this IEvent @event) =>
            @event.IfTypeIs<DynamicEvent>()
                  .Then(e => e.EventStreamName)
                  .Else(() => EventStreamName(@event.AggregateType()));

        /// <summary>
        /// Gets the name of the event stream (e.g. the aggregate type name) to which the specified event type belongs.
        /// </summary>
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