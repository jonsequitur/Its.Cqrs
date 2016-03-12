// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Its.Domain.Testing
{
    internal class InMemoryCommandETagStore
    {
        private readonly ConcurrentDictionary<Tuple<string, string>, DateTimeOffset> dictionary = new ConcurrentDictionary<Tuple<string, string>, DateTimeOffset>();

        public bool TryAdd(string scope, string etag) =>
            dictionary.TryAdd(Tuple.Create(scope, etag), DateTimeOffset.Now);
    }
}