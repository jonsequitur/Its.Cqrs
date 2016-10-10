// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Serialization
{
    [DebuggerStepThrough]
    internal class OptionalConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, Func<object, IOptional>> factories = new ConcurrentDictionary<Type, Func<object, IOptional>>();

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            value.IfTypeIs<IOptional>()
                 .ThenDo(optional =>
                 {
                     if (optional.IsSet)
                     {
                         writer.WriteRawValue(optional.Value.ToJson());
                     }
                 });
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.
        /// </summary>
        /// <value><c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON; otherwise, <c>false</c>.</value>
        public override bool CanRead => true;

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var mainType = objectType.GetGenericArguments().Single();
            var deserialized = serializer.Deserialize(reader, mainType);
            return factories.GetOrAdd(objectType, t => o => t.Member().Create(o)).Invoke(deserialized);
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType) => typeof (IOptional).IsAssignableFrom(objectType);
    }
}
