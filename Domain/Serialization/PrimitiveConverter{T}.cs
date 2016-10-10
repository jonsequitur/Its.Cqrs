// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Serialization
{
    /// <summary>
    /// Converts to and from single-field JSON values.
    /// </summary>
    /// <typeparam name="T">The type that can be converted.</typeparam>
    /// <seealso cref="Newtonsoft.Json.JsonConverter" />
    public class PrimitiveConverter<T> : JsonConverter
    {
        private readonly Func<T, object> serialize;
        private readonly Func<object, T> deserialize;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveConverter{T}"/> class.
        /// </summary>
        /// <param name="serialize">The serialize delegate.</param>
        /// <param name="deserialize">The deserialize delegate.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public PrimitiveConverter(Func<T, object> serialize, Func<object, T> deserialize)
        {
            if (deserialize == null)
            {
                throw new ArgumentNullException(nameof(deserialize));
            }
            if (serialize == null)
            {
                throw new ArgumentNullException(nameof(serialize));
            }
            this.deserialize = deserialize;
            this.serialize = serialize;
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            writer.WriteValue(serialize((T) value));

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return JToken.ReadFrom(reader).ToObject(objectType);
            }

            return deserialize(reader.Value);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.
        /// </summary>
        /// <value><c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON; otherwise, <c>false</c>.</value>
        public override bool CanRead => true;

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType) => objectType == typeof (T);
    }
}
