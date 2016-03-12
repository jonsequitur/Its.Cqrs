// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    internal class MatchEvent
    {
        public const string Wildcard = "*";

        private readonly string type;
        private readonly string streamName;

        public MatchEvent(string type = Wildcard, string streamName = "")
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (streamName == null)
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            if (type == "IEvent" || type == "Event")
            {
                type = Wildcard;
            }
            this.type = type;

            this.streamName = streamName;
        }

        public string Type => type;

        public string StreamName => streamName;

        public bool Matches(IEvent @event) =>
            (streamName == "" || streamName == Wildcard || streamName.Equals(@event.EventStreamName())) &&
            (type == "" || type == Wildcard || type.Equals(@event.EventName()));

        public override string ToString() =>
            $"{StreamName}.{Type}";

        protected bool Equals(MatchEvent other) =>
            string.Equals(Type, other.Type) && string.Equals(StreamName, other.StreamName);

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
            return Equals((MatchEvent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type?.GetHashCode() ?? 0)*397) ^ (StreamName?.GetHashCode() ?? 0);
            }
        }

        public static MatchEvent[] All =
        {
            new MatchEvent()
        };
    }
}