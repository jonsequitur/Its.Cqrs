// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
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