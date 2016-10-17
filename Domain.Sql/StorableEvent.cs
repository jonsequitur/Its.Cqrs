// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    ///     Model for storing events in a SQL database using Entity Framework.
    /// </summary>
    [Table("Events", Schema = "EventStore")]
    public class StorableEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public StorableEvent()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        ///     Gets or sets the incrementing database-assigned identifier which can be used as a checkpoint for building
        ///     projections.
        /// </summary>
        [Index(IsUnique = true), Index("IX_Id_StreamName_Type", 1, IsUnique = true)]
        public long Id { get; set; }

        /// <summary>
        ///     Gets or sets the name of the stream, e.g. the aggregate's type name.
        /// </summary>
        [MaxLength(50), Index("IX_Id_StreamName_Type", 2, IsUnique = true)]
        public string StreamName { get; set; }

        /// <summary>
        ///     Gets or sets the name of the event type.
        /// </summary>
        [MaxLength(100), Index("IX_Id_StreamName_Type", 3, IsUnique = true)]
        public string Type { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="DateTime" /> (as UTC) time representation of the event's <see cref="Timestamp" />.
        /// </summary>
        public DateTime UtcTime
        {
            get
            {
                return new DateTime(Timestamp.UtcDateTime.Ticks);
            }
            set
            {
                Timestamp =
                    value.Kind == DateTimeKind.Unspecified
                        ? new DateTime(value.Ticks, DateTimeKind.Utc)
                        : value;
            }
        }

        /// <summary>
        ///     Gets or sets a string representing the actor within the system that was operating on the aggregate when the event
        ///     was recorded.
        /// </summary>
        [MaxLength(255)]
        public string Actor { get; set; }

        /// <summary>
        ///     Gets or sets the serialized body of the domain event.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        ///     Gets or sets the time at which the event was originally recorded..
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        ///     Gets the id of the aggregate to which this event applies.
        /// </summary>
        public Guid AggregateId { get; set; }

        /// <summary>
        ///     Gets the position of the event within the source object's event sequence.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        ///     Gets the event's ETag, which is used to support idempotency within the event stream.
        /// </summary>
        [MaxLength(100), Index]
        public string ETag { get; set; }
    }
}