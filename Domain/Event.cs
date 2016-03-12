// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;
#pragma warning disable 618

namespace Microsoft.Its.Domain
{
    /// <summary>
    ///     An event signifying some change in the domain.
    /// </summary>
    public class Event : IEvent, IHaveExtensibleMetada
    {
        private dynamic metadata;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Event" /> class.
        /// </summary>
        protected Event()
        {
            var commandContext = CommandContext.Current;

            if (commandContext != null)
            {
                var command = commandContext.Command;
                this.SetActor(command);
                ETag = command.ETag;
            }

            Timestamp = Clock.Now();
        }

        /// <summary>
        ///     Gets the position of the event within the event stream.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        ///     Gets the unique id of the source object to which this event applies.
        /// </summary>
        public Guid AggregateId { get; set; }

        /// <summary>
        /// Gets the time at which the event was originally recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the event's ETag, which is used to support idempotency within the event stream.
        /// </summary>
        public string ETag { get; set; }

        internal static readonly Type[] HandlerGenericTypeDefinitions =
        {
            typeof (IUpdateProjectionWhen<>),
            typeof (IHaveConsequencesWhen<>)
        };

        /// <summary>
        /// Gets a dynamic metadata object that can be used to pass extensibility information along with the event.
        /// </summary>
        [JsonIgnore]
        public dynamic Metadata => metadata ?? (metadata = new ExpandoObject());

        /// <summary>
        /// Returns the known concrete event types for the specified aggregate type.
        /// </summary>
        /// <param name="aggregateType">Type of the aggregate.</param>
        public static IEnumerable<Type> KnownTypesForAggregateType(Type aggregateType) =>
            typeof (Event<>).MakeGenericType(aggregateType)
                            .Member()
                            .KnownTypes;

        /// <summary>
        /// Returns all known concrete event types.
        /// </summary>
        public static IEnumerable<Type> KnownTypes() => Discover.ConcreteTypesDerivedFrom(typeof (IEvent));

        internal static Type[] ConcreteTypesOf(Type eventType)
        {
            var types = Discover.ConcreteTypesDerivedFrom(eventType);

            if (!typeof (IScheduledCommand).IsAssignableFrom(eventType))
            {
                return types.ToArray();
            }

            if (eventType.IsGenericType)
            {
                var type = typeof (CommandScheduled<>).MakeGenericType(eventType.GetGenericArguments());
                return types.Concat(new[] { type })
                            .Distinct()
                            .ToArray();
            }

            return types.Concat(AggregateType.KnownTypes.Select(t => typeof (CommandScheduled<>).MakeGenericType(t)).ToArray())
                        .Distinct()
                        .ToArray();
        }
    }
}
