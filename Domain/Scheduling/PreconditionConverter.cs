// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain
{
    internal class PreconditionConverter : JsonConverter
    {
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<CommandPrecondition>(reader);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (IPrecondition);
        }
    }
}