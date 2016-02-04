// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    public class InMemoryStore<T> : IStore<T>, IEnumerable<T>
        where T : class
    {
        private readonly ConcurrentDictionary<string, T> dictionary = new ConcurrentDictionary<string, T>();

        private readonly Func<T, string> getId;
        private readonly Func<string, T> create;

        public InMemoryStore(Func<T, string> getId = null, Func<string, T> create = null)
        {
            this.create = create;

            if (getId != null)
            {
                this.getId = getId;
            }
            else
            {
                this.getId = t => t.GetHashCode().ToString();
            }
        }

        public Task<T> Get(string id)
        {
            T value;

            dictionary.TryGetValue(id, out value);

            if (value == null && create != null)
            {
                value = create(id);
            }

            return Task.FromResult(value);
        }

        public Task Put(T value)
        {
            dictionary.TryAdd(getId(value), value);

            return Task.FromResult(Unit.Default);
        }

        public void Add(T value)
        {
            Put(value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}