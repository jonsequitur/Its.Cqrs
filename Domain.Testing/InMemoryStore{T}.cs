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
    /// <summary>
    /// Simulates storage in memory for entities that can have commands applied to them by the command scheduler.
    /// </summary>
    public class InMemoryStore<T> : IStore<T>, IEnumerable<T>
        where T : class
    {
        private readonly ConcurrentDictionary<string, T> dictionary = new ConcurrentDictionary<string, T>();

        private readonly Func<T, string> getId;
        private readonly Func<string, T> create;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryStore{T}"/> class.
        /// </summary>
        /// <param name="getId">The get identifier.</param>
        /// <param name="create">The create.</param>
        public InMemoryStore(Func<T, string> getId = null, Func<string, T> create = null)
        {
            this.create = create;

            if (getId != null)
            {
                this.getId = getId;
            }
            else
            {
                this.getId = t => ((dynamic)t).Id;
            }
        }

        /// <summary>
        ///     Gets a command target by the id.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
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

        /// <summary>
        ///     Persists the state of the command target.
        /// </summary>
        public Task Put(T value)
        {
            dictionary.TryAdd(getId(value), value);

            return Task.FromResult(Unit.Default);
        }

        /// <summary>
        /// Adds the specified value to the store.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Add(T value) => Put(value);

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator() => dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}