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
    public class EventContractResolver : DefaultContractResolver
    {
        /// <inheritdoc />
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            ApplyRulesToProperty(property);

            return property;
        }

        /// <summary>
        /// Applies rules for serializing events to the specified property.
        /// </summary>
        /// <param name="property">The property to apply rules to.</param>
        public static void ApplyRulesToProperty(JsonProperty property)
        {
            Serialization.OptionalContractResolver.ApplyRulesToProperty(property);

            if (typeof(Event).IsAssignableFrom(property.DeclaringType)
                && (property.PropertyName == "SequenceNumber" ||
                    property.PropertyName == "Timestamp" ||
                    property.PropertyName == "AggregateId" ||
                    property.PropertyName == "ETag"))
            {
                property.Ignored = true;
            }
        }
    }
}
