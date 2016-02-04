// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ETag
    {
        public long Id { get; set; }

        [Index("IX_AggregateId_and_ETagValue", 1, IsUnique = true)]
        public Guid AggregateId { get; set; }

        [Index("IX_AggregateId_and_ETagValue", 2, IsUnique = true)]
        public string ETagValue { get; set; }

        public DateTimeOffset CreatedTime { get; set; }
    }
}