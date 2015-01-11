// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Helps deserialize domain events when the body does not contain certain properties, which are pulled from SQL table columns instead.
    /// </summary>
    [DebuggerStepThrough]
    internal class EventContractResolver : Serialization.OptionalContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (typeof (Event).IsAssignableFrom(property.DeclaringType)
                && (property.PropertyName == "SequenceNumber" ||
                    property.PropertyName == "Timestamp" ||
                    property.PropertyName == "AggregateId" || 
                    property.PropertyName == "ETag"))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
