// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;

namespace Microsoft.Its.Domain.Testing
{
    public class InMemoryStoredEvent : IHaveExtensibleMetada
    {
        private dynamic metadata;

        public InMemoryStoredEvent()
        {
            Timestamp = Clock.Now();
            metadata = new ExpandoObject();
            metadata.AbsoluteSequenceNumber = 0;
        }

        public string Body { get; set; }

        public string ETag { get; set; }

        public string AggregateId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string StreamName { get; set; }

        public string Type { get; set; }

        public long SequenceNumber { get; set; }

        public dynamic Metadata => metadata;

        protected bool Equals(InMemoryStoredEvent other) =>
            string.Equals(AggregateId, other.AggregateId, StringComparison.OrdinalIgnoreCase) && SequenceNumber == other.SequenceNumber;

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
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((InMemoryStoredEvent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(AggregateId)*397) ^ SequenceNumber.GetHashCode();
            }
        }

        public static bool operator ==(InMemoryStoredEvent left, InMemoryStoredEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(InMemoryStoredEvent left, InMemoryStoredEvent right)
        {
            return !Equals(left, right);
        }
    }
}