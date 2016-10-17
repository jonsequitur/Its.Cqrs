// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// Storage model representing scheduled command etags.
    /// </summary>
    public class ETag
    {
        /// <summary>
        /// Gets or sets the identifier for the etag.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the scope within which the etag is expected to be unique.
        /// </summary>
        [Index("IX_Scope_and_ETag", 1, IsUnique = true)]
        [MaxLength(50)]
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the etag value.
        /// </summary>
        [Index("IX_Scope_and_ETag", 2, IsUnique = true)]
        [MaxLength(50)]
        public string ETagValue { get; set; }

        /// <summary>
        /// Gets or sets the domain time at which the etag was created.
        /// </summary>
        public DateTimeOffset CreatedDomainTime { get; set; }

        /// <summary>
        /// Gets or sets the actual time at which the etag was created.
        /// </summary>
        public DateTimeOffset CreatedRealTime { get; set; }
    }
}