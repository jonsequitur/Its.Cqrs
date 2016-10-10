// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Its.Domain.Serialization
{
    /// <summary>
    /// Used by Newtonsoft.Json.JsonSerializer to resolves a Newtonsoft.Json.Serialization.JsonContract for <see cref="Optional{T}" /> values.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.Serialization.DefaultContractResolver" />
    [DebuggerStepThrough]
    public class OptionalContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty"/> for the given <see cref="T:System.Reflection.MemberInfo"/>.
        /// </summary>
        /// <param name="memberSerialization">The member's parent <see cref="T:Newtonsoft.Json.MemberSerialization"/>.</param><param name="member">The member to create a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty"/> for.</param>
        /// <returns>
        /// A created <see cref="T:Newtonsoft.Json.Serialization.JsonProperty"/> for the given <see cref="T:System.Reflection.MemberInfo"/>.
        /// </returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (typeof (IOptional).IsAssignableFrom(property.PropertyType))
            {
                property.NullValueHandling = NullValueHandling.Include;

                property.ShouldSerialize = obj =>
                {
                    var optional = (IOptional) property.ValueProvider.GetValue(obj);
                    return optional.IsSet;
                };
            }
            else if (typeof (IPrincipal).IsAssignableFrom(property.PropertyType))
            {
                property.Ignored = true;
            }

            return property;
        }
    }
}
