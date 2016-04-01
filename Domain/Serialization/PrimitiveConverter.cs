// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Serialization
{
    internal class PrimitiveConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type,Func<JToken,object>> deserializers = new ConcurrentDictionary<Type, Func<JToken, object>>();  
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => 
            writer.WriteValue(((dynamic)value).Value);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jToken = JToken.ReadFrom(reader);
            if (jToken == null || string.IsNullOrEmpty(jToken.ToString()))
            {
                return null;
            }
            var deserialize = deserializers.GetOrAdd(objectType,CreateDeserializer);
            return deserialize(jToken);
        }

        private static Func<JToken, object> CreateDeserializer(Type objectType)
        {
            var ctor = objectType.GetConstructors()
                                 .Single(c => c.GetParameters()
                                               .Length == 1);
            
            var jt = Expression.Parameter(typeof (JToken),"jt");
            var ctorParamType = ctor.GetParameters()
                                    .Single()
                                    .ParameterType;

            var getValue = typeof (PrimitiveConverter).GetMethod("GetValue", BindingFlags.NonPublic | BindingFlags.Static)
                                                      .MakeGenericMethod(ctorParamType);
            var call = Expression.Call(getValue,jt);
            var invokeCtor = Expression.New(ctor,call);
            var lambda = Expression.Lambda<Func<JToken, object>>(invokeCtor, jt);

            return lambda.Compile();
        }

        private static T GetValue<T>(JToken jToken) =>
            jToken.Type == JTokenType.Object
                ? jToken["Value"].ToObject<T>()
                : jToken.ToObject<T>();

        public override bool CanConvert(Type objectType) => true;
    }
}