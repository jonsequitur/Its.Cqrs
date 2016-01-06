using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Serialization
{
    internal class StringTConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jToken = JToken.ReadFrom(reader);

            string value;

            if (jToken.Type == JTokenType.String)
            {
                value = jToken.ToString();
            }
            else
            {
                value = jToken.Value<string>("Value");
            }

            return Activator.CreateInstance(objectType, value);
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}