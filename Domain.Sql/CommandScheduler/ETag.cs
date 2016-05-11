// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ETag
    {
        public long Id { get; set; }

        [Index("IX_Scope_and_ETag", 1, IsUnique = true)]
        [MaxLength(50)]
        public string Scope { get; set; }

        [Index("IX_Scope_and_ETag", 2, IsUnique = true)]
        [MaxLength(50)]
        public string ETagValue { get; set; }

        public DateTimeOffset CreatedDomainTime { get; set; }

        public DateTimeOffset CreatedRealTime { get; set; }
    }
}