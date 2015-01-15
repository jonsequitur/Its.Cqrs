// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;

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

        internal static readonly Type[] HandlerGenericTypeDefinitions = new[]
        {
            typeof (IUpdateProjectionWhen<>),
            typeof (IHaveConsequencesWhen<>)
        };

        /// <summary>
        /// Gets a dynamic metadata object that can be used to pass extensibility information along with the event.
        /// </summary>
        [JsonIgnore]
        public dynamic Metadata
        {
            get
            {
                return metadata ?? (metadata = new ExpandoObject());
            }
        }

        /// <summary>
        /// Returns the known event types for the specified aggregate type.
        /// </summary>
        /// <param name="aggregateType">Type of the aggregate.</param>
        public static IEnumerable<Type> KnownTypesForAggregateType(Type aggregateType)
        {
            return typeof (Event<>).MakeGenericType(aggregateType)
                                   .Member()
                                   .KnownTypes;
        }

        internal static Type[] ConcreteTypesOf(Type eventType)
        {
            return Discover.ConcreteTypesDerivedFrom(eventType)
                           .Concat(new[] { eventType })
                           .Distinct()
                           .ToArray();
        }
    }
}
