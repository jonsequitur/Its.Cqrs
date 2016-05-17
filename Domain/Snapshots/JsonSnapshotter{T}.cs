// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Its.Domain
{
    internal class JsonSnapshotter<T> : 
        ICreateSnapshot<T>,
        IApplySnapshot<T>
        where T : class, IEventSourced
    {
        private static readonly JsonSerializerSettings serializationSettings = new JsonSerializerSettings
        {
            ContractResolver = new PrivateStateContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.All
        };

        public ISnapshot CreateSnapshot(T aggregate)
        {
            var snapshot = new JsonSnapshot();

            aggregate.InitializeSnapshot(snapshot);

            snapshot.Body = JsonConvert.SerializeObject(aggregate, serializationSettings);

            return snapshot;
        }

        public void ApplySnapshot(ISnapshot snapshot, T aggregate)
        {
            JsonConvert.PopulateObject(
                snapshot.Body, 
                aggregate, 
                serializationSettings);
        }

        public class PrivateStateContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(
                Type type,
                MemberSerialization memberSerialization)
            {
                var bindingFlags = BindingFlags.Public |
                                   BindingFlags.NonPublic |
                                   BindingFlags.Instance;

                var properties = type.GetProperties(bindingFlags);
                var fields = type.GetFields(bindingFlags).Cast<MemberInfo>();

                return properties
                    .Concat(fields)
                    .Select(p =>
                            CreateProperty(p, memberSerialization))
                    .Do(p =>
                    {
                        p.Writable = true;
                        p.Readable = true;
                    })
                    .ToList();
            }
        }
    }
}