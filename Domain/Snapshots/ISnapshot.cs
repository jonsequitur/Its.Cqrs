// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    public interface ISnapshot
    {
        Guid AggregateId { get; set; }
        long Version { get; set; }
        DateTimeOffset LastUpdated { get; set; }
        string AggregateTypeName { get; set; }
        string[] ETags { get; set; }
    }
}