// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            DefaultValueHandling = DefaultValueHandling.Ignore
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
            JsonConvert.PopulateObject(snapshot.Body, aggregate, serializationSettings);
        }

        public class PrivateStateContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => CreateProperty(p, memberSerialization))
                                .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                           .Select(f => CreateProperty(f, memberSerialization)))
                                .ToList();

                props.ForEach(p =>
                {
                    p.Writable = true;
                    p.Readable = true;
                });

                return props;
            }
        }
    }

    internal interface IApplySnapshot<T> 
         where T : class, IEventSourced
    {
        void ApplySnapshot(ISnapshot snapshot, T aggregate);
    }

    [DebuggerStepThrough]
    internal class JsonSnapshot : ISnapshot
    {
        public Guid AggregateId { get; set; }
        public long Version { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public string AggregateTypeName { get; set; }
        public BloomFilter ETags { get; set; }
        public string Body { get; set; }
    }
}