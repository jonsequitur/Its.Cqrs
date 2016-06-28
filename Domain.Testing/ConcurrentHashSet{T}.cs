// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Its.Domain.Testing
{
    internal class ConcurrentHashSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, byte> dictionary = new ConcurrentDictionary<T, byte>();

        public void Add(T value) => dictionary.TryAdd(value, 0);

        public IEnumerator<T> GetEnumerator() => dictionary.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}