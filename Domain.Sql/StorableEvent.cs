// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// An event wrapper for writing events to a SQL databsae using Entity Framework.
    /// </summary>
    public class StorableEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public StorableEvent()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        public long Id { get; set; }

        public string StreamName { get; set; }

        public string Type { get; set; }

        public DateTime UtcTime
        {
            get
            {
                return new DateTime(Timestamp.UtcDateTime.Ticks);
            }
            set
            {
                if (value.Kind == DateTimeKind.Unspecified)
                {
                    // treat it as Utc
                    Timestamp = new DateTime(value.Ticks, DateTimeKind.Utc);
                }
                else
                {
                    Timestamp = value;
                }
            }
        }

        public string Actor { get; set; }

        /// <summary>
        /// Gets or sets the serialized body of the actual event.
        /// </summary>
        public string Body { get; set; }

        public DateTimeOffset Timestamp { get; set; } 

        public Guid AggregateId { get; set; }

        public long SequenceNumber { get; set; }

        public string ETag { get; set; }
    }
}