// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Its.Domain
{
    [DebuggerDisplay("{ToString()}")]
    public class ScheduledCommandPrecondition
    {
        public Guid AggregateId { get; set; }
        public string ETag { get; set; }

        public override string ToString()
        {
            return string.Format("{0}...{1}",
                                 AggregateId.ToString().Substring(0, 4),
                                 ETag);
        }
    }
}