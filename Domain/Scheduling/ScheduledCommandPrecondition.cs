// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    public class ScheduledCommandPrecondition
    {
        public Guid AggregateId { get; set; }
        public string ETag { get; set; }
    }
}