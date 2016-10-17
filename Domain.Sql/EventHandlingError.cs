// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides information about an error that occurs while handling an event.
    /// </summary>
    public class EventHandlingError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandlingError"/> class.
        /// </summary>
        public EventHandlingError()
        {
            UtcTime = Clock.Now();
        }

        /// <summary>
        /// Returns a string representing the actor within the system that was operating on the aggregate when the event was recorded.
        /// </summary>
        public string Actor { get; set; }

        /// <summary>
        /// Gets or sets the name of the handler that encountered an error.
        /// </summary>
        public string Handler { get; set; }

        /// <summary>
        /// Gets or sets the sequence number of the event that was being handled when the error occurred.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// Gets or sets the aggregate id of the event that was being handled when the error occurred.
        /// </summary>
        public Guid AggregateId { get; set; }

        /// <summary>
        /// Gets or sets the id of error.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the stream name of the event that was being handled when the error occurred.
        /// </summary>
        public string StreamName { get; set; }

        /// <summary>
        /// Gets or sets the event type name of the event that was being handled when the error occurred.
        /// </summary>
        public string EventTypeName { get; set; }

        /// <summary>
        /// Gets or sets the UTC time at which the error occurred.
        /// </summary>
        public DateTimeOffset UtcTime { get; set; }

        /// <summary>
        /// Gets or sets the serialized event that was being handled when the error occurred.
        /// </summary>
        public string SerializedEvent { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the event id of the event that was being handled when the error occurred.
        /// </summary>
        public long? OriginalId { get; set; }
    }
}