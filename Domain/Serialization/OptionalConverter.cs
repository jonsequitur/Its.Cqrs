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

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var mainType = objectType.GetGenericArguments().Single();
            var deserialized = serializer.Deserialize(reader, mainType);
            return factories.GetOrAdd(objectType, t => o => t.Member().Create(o)).Invoke(deserialized);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof (IOptional).IsAssignableFrom(objectType);
        }
    }
}
