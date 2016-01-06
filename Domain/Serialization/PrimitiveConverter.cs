using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((dynamic)value).Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var deserialize = deserializers.GetOrAdd(objectType,CreateDeserializer);
            var jToken = JToken.ReadFrom(reader);
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

            var getValue = typeof (PrimitiveConverter).GetMethod("GetValue",BindingFlags.NonPublic | BindingFlags.Static)
                                                      .MakeGenericMethod(ctorParamType);
            var call = Expression.Call(getValue,jt);
            var invokeCtor = Expression.New(ctor,call);
            var lambda = Expression.Lambda<Func<JToken, object>>(invokeCtor,jt);

            return lambda.Compile();
        }

        private static T GetValue<T>(JToken jToken)
        {
            return jToken.Type == JTokenType.Object
                ? jToken.Value<T>("Value")
                : jToken.Value<T>();
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}