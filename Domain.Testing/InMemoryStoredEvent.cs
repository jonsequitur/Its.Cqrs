// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// An in-memory stored event model.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.IHaveExtensibleMetada" />
    public class InMemoryStoredEvent : IHaveExtensibleMetada
    {
        private readonly dynamic metadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryStoredEvent"/> class.
        /// </summary>
        public InMemoryStoredEvent()
        {
            Timestamp = Clock.Now();
            metadata = new ExpandoObject();
            metadata.AbsoluteSequenceNumber = 0;
        }

        /// <summary>
        ///     Gets or sets the serialized body of the domain event.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Gets the event's ETag, which is used to support idempotency within the event stream.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets the id of the aggregate to which this event applies.
        /// </summary>
        public string AggregateId { get; set; }

        /// <summary>
        /// Gets the time at which the event was originally recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the name of the stream, e.g. the aggregate's type name.
        /// </summary>
        public string StreamName { get; set; }

        /// <summary>
        /// Gets or sets the name of the event type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets the position of the event within the source object's event sequence.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// Gets a dynamic metadata object that can be used to pass extensibility information along with the event.
        /// </summary>
        public dynamic Metadata => metadata;

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        protected bool Equals(InMemoryStoredEvent other) =>
            string.Equals(AggregateId, other.AggregateId, StringComparison.OrdinalIgnoreCase) && SequenceNumber == other.SequenceNumber;

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((InMemoryStoredEvent) obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(AggregateId)*397) ^ SequenceNumber.GetHashCode();
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(InMemoryStoredEvent left, InMemoryStoredEvent right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(InMemoryStoredEvent left, InMemoryStoredEvent right)
        {
            return !Equals(left, right);
        }
    }
}