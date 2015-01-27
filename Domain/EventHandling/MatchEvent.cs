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
                throw new ArgumentNullException("type");
            }
            if (streamName == null)
            {
                throw new ArgumentNullException("streamName");
            }

            if (type == "IEvent" || type == "Event")
            {
                type = Wildcard;
            }
            this.type = type;
            
            this.streamName = streamName;
        }

        public string Type
        {
            get
            {
                return type;
            }
        }

        public string StreamName
        {
            get
            {
                return streamName;
            }
        }

        public bool Matches(IEvent @event)
        {
            return (streamName == "" || streamName == Wildcard || streamName.Equals(@event.EventStreamName())) &&
                   (type == "" || type == Wildcard || type.Equals(@event.EventName()));
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", StreamName, Type);
        }

        protected bool Equals(MatchEvent other)
        {
            return string.Equals(Type, other.Type) && string.Equals(StreamName, other.StreamName);
        }

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
                return ((Type != null ? Type.GetHashCode() : 0)*397) ^ (StreamName != null ? StreamName.GetHashCode() : 0);
            }
        }

        public static MatchEvent[] All =
        {
            new MatchEvent()
        };
    }
}
