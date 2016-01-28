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

        private readonly Func<T, string> getId = t => t.GetHashCode().ToString();

        public InMemoryStore(Func<T, string> getId = null)
        {
            if (getId != null)
            {
                this.getId = getId;
            }
        }

        public Task<T> Get(string id)
        {
            T value;

            dictionary.TryGetValue(id, out value);

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