// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Serialization
{
    internal class DynamicEvent : JObject, IEvent
    {
        public DynamicEvent(JObject other) : base(other)
        {
        }

        public string EventStreamName
        {
            get
            {
                return base.Value<string>("EventStreamName");
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
                return base.Value<string>("EventTypeName");
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
                return base.Value<long>("SequenceNumber");
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
                return base.Value<DateTimeOffset>("Timestamp");
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
                return base.Value<string>("ETag");
            }
            set
            {
                base["ETag"] = value;
            }
        }
    }
}