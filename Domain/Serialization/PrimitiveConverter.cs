// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Serialization
{
    public class PrimitiveConverter<T> : JsonConverter
    {
        protected Func<T, object> serialize;
        protected Func<object, T> deserialize;

        public PrimitiveConverter(Func<T, object> serialize, Func<object, T> deserialize)
        {
            if (deserialize == null)
            {
                throw new ArgumentNullException("deserialize");
            }
            if (serialize == null)
            {
                throw new ArgumentNullException("serialize");
            }
            this.deserialize = deserialize;
            this.serialize = serialize;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(serialize((T) value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return JToken.ReadFrom(reader).ToObject(objectType);
            }

            return deserialize(reader.Value);
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (T);
        }
    }
}
