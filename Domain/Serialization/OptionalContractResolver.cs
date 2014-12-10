using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Its.Domain.Serialization
{
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