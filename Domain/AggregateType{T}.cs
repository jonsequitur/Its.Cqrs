// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides information about an aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    public static class AggregateType<TAggregate> where TAggregate : IEventSourced
    {
        public static readonly Func<Guid, IEnumerable<IEvent>, TAggregate> FromEventHistory = CallEventHistoryConstructor();
        
        public static readonly Func<ISnapshot, IEnumerable<IEvent>, TAggregate> FromSnapshot = CallSnapshotConstructor();

        /// <summary>
        /// Gets the name of the event stream where this aggregate type's events are stored.
        /// </summary>
        public static string EventStreamName { get; } = typeof (TAggregate).Name;

        /// <summary>
        /// Gets a value indicating whether the aggregate type supports instantiation via snapshots.
        /// </summary>
        public static bool SupportsSnapshots => FromSnapshot != null;

        private static Func<Guid, IEnumerable<IEvent>, TAggregate> CallEventHistoryConstructor()
        {
            var constructor = typeof (TAggregate).GetConstructor(new[] { typeof (Guid), typeof (IEnumerable<IEvent>) });

            if (constructor == null)
            {
                throw new ArgumentException(
                    string.Format(
                        "No constructor found for type '{0}' having the signature {0}(Guid id, IEnumerable<IEvent> eventHistory), which is required sourcing from events.",
                        typeof (TAggregate).Name));
            }

            var parameterExpressions = new[]
                                       {
                                           Expression.Parameter(typeof (Guid), "id"),
                                           Expression.Parameter(typeof (IEnumerable<IEvent>), "eventHistory")
                                       };

            var callConstructor = Expression.Lambda<Func<Guid, IEnumerable<IEvent>, TAggregate>>(
                Expression.New(constructor,
                               parameterExpressions.ToArray()),
                parameterExpressions).Compile();

            return (id, events) => callConstructor(id, events);
        }

        private static Func<ISnapshot, IEnumerable<IEvent>, TAggregate> CallSnapshotConstructor()
        {
            var constructors = typeof (TAggregate).GetConstructors();
            var constructor = constructors.SingleOrDefault(ctor =>
                                                           {
                                                               var types = ctor.GetParameters()
                                                                               .Select(p => p.ParameterType)
                                                                               .ToArray();

                                                               return types.Length == 2 &&
                                                                      types[1] == typeof (IEnumerable<IEvent>) &&
                                                                      typeof (ISnapshot).IsAssignableFrom(types[0]);
                                                           });

            if (constructor == null)
            {
                return null;
            }

            var typeSpecificSnapshotParameter = Expression.Parameter(constructor.GetParameters().First().ParameterType, "snapshot");
            var eventHistoryParameter = Expression.Parameter(typeof (IEnumerable<IEvent>), "eventHistory");
            var interfaceSnapshotParameter = Expression.Parameter(typeof (ISnapshot), "snapshot");

            var ctorCallExpression = Expression.New(constructor,
                                                    Expression.TypeAs(interfaceSnapshotParameter, typeSpecificSnapshotParameter.Type),
                                                    eventHistoryParameter);

            var callConstructor = Expression.Lambda<Func<ISnapshot, IEnumerable<IEvent>, TAggregate>>(
                ctorCallExpression,
                interfaceSnapshotParameter,
                eventHistoryParameter).Compile();

            return (snapshot, events) => callConstructor(snapshot, events);
        }
    }
}
