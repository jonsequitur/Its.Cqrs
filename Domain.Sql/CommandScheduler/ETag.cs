// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ETag
    {
        public Guid AggregateId { get; set; }

        public string ETagValue { get; set; }

        public DateTimeOffset CreatedTime { get; set; } 
    }
}