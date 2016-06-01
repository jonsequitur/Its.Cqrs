// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Serialization
{
    internal class DynamicEvent : JObject, IEvent, IHaveExtensibleMetada
    {
        private dynamic metadata;

        public DynamicEvent(JObject other) : base(other)
        {
        }

        public string EventStreamName
        {
            get
            {
                return Value<string>("EventStreamName");
            }
            set
            {
                base["EventStreamName"] = value;
            }
        }

        public string EventTypeName
        {
            get
            {
                return Value<string>("EventTypeName");
            }
            set
            {
                base["EventTypeName"] = value;
            }
        }

        public long SequenceNumber
        {
            get
            {
                return Value<long>("SequenceNumber");
            }
            set
            {
                base["SequenceNumber"] = value;
            }
        }

        public Guid AggregateId
        {
            get
            {
                JToken jtoken;
                if (TryGetValue("AggregateId", out jtoken))
                {
                    return Guid.Parse(jtoken.Value<string>());
                }

                return default(Guid);
            }
            set
            {
                base["AggregateId"] = value.ToString();
            }
        }

        public DateTimeOffset Timestamp
        {
            get
            {
                return Value<DateTimeOffset>("Timestamp");
            }
            set
            {
                base["Timestamp"] = value;
            }
        }

        public string ETag
        {
            get
            {
                return Value<string>("ETag");
            }
            set
            {
                base["ETag"] = value;
            }
        }

        /// <summary>
        /// Gets a dynamic metadata object that can be used to pass extensibility information along with the event.
        /// </summary>
        [JsonIgnore]
        public dynamic Metadata => metadata ?? (metadata = new ExpandoObject());
    }
}
