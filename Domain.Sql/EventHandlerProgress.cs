// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    internal class EventHandlerProgress
    {
        public string Name { get; set; }

        public long? InitialCatchupEvents { get; set; }

        public TimeSpan? TimeTakenForInitialCatchup { get; set; }

        public TimeSpan? TimeRemainingForCatchup { get; set; }

        public double LatencyInMilliseconds { get; set; }

        public long? EventsRemainingInBatch { get; set; }

        public decimal? PercentageCompleted { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public long CurrentAsOfEventId { get; set; }

        public long? FailedOnEventId { get; set; }

        public string Error { get; set; }
    }
}
