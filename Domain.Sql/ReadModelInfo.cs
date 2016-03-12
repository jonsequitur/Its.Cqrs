// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    public class ReadModelInfo
    {
        public string Name { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public long CurrentAsOfEventId { get; set; }

        public long? FailedOnEventId { get; set; }

        public string Error { get; set; }

        public double LatencyInMilliseconds { get; set; }

        public DateTimeOffset? InitialCatchupStartTime { get; set; }

        public long InitialCatchupEvents { get; set; }

        public DateTimeOffset? InitialCatchupEndTime { get; set; }

        public long BatchRemainingEvents { get; set; }

        public DateTimeOffset? BatchStartTime { get; set; }

        public long BatchTotalEvents { get; set; }

        public static string NameForProjector(object projector) => EventHandler.FullName(projector);
    }
}
