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

        public string Actor { get; set; }

        public string Handler { get; set; }

        public long SequenceNumber { get; set; }

        public Guid AggregateId { get; set; }

        public long Id { get; set; }

        public string StreamName { get; set; }

        public string EventTypeName { get; set; }

        public DateTimeOffset UtcTime { get; set; }

        public string SerializedEvent { get; set; }

        public string Error { get; set; }

        public long? OriginalId { get; set; }
    }
}