// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain.Sql
{
    internal class EventHandlerProgress
    {
        private readonly DateTimeOffset now;
        private readonly ReadModelInfo readModelInfo;

        public EventHandlerProgress(
            ReadModelInfo readModelInfo,
            DateTimeOffset? asOf = null)
        {
            if (readModelInfo == null)
            {
                throw new ArgumentNullException(nameof(readModelInfo));
            }

            this.readModelInfo = readModelInfo;

            now = asOf ?? Clock.Now();
        }

        public string Name => readModelInfo.Name;

        public long BatchEventsProcessed =>
            readModelInfo.BatchTotalEvents - readModelInfo.BatchRemainingEvents;

        public decimal? BatchPercentageCompleted => Percent(
            BatchEventsProcessed,
            readModelInfo.BatchTotalEvents);

        public long? BatchRemainingEvents => readModelInfo.BatchRemainingEvents;

        public TimeSpan BatchTimeRemaining => TimeRemaining(
            (now - readModelInfo.BatchStartTime).Value,
            BatchEventsProcessed,
            readModelInfo.BatchRemainingEvents);

        public long BatchTotalEvents => readModelInfo.BatchTotalEvents;

        public long InitialCatchupEventsProcessed =>
            readModelInfo.InitialCatchupTotalEvents - readModelInfo.InitialCatchupRemainingEvents;

        public long InitialCatchupRemainingEvents => readModelInfo.InitialCatchupRemainingEvents;

        public TimeSpan? InitialCatchupTimeElapsed =>
            readModelInfo.InitialCatchupEndTime.HasValue
                ? readModelInfo.InitialCatchupEndTime - readModelInfo.InitialCatchupStartTime
                : now - readModelInfo.InitialCatchupStartTime.Value;

        public TimeSpan? InitialCatchupTimeRemaining => TimeRemaining(
            (now - readModelInfo.InitialCatchupStartTime).Value,
            InitialCatchupEventsProcessed,
            readModelInfo.InitialCatchupRemainingEvents);

        public long? InitialCatchupTotalEvents => readModelInfo.InitialCatchupTotalEvents;

        public double LatencyInMilliseconds => readModelInfo.LatencyInMilliseconds;

        public decimal? InitialCatchupPercentageCompleted => Percent(
            InitialCatchupEventsProcessed,
            readModelInfo.InitialCatchupTotalEvents);

        public DateTimeOffset? LastUpdated => readModelInfo.LastUpdated;

        public long CurrentAsOfEventId => readModelInfo.CurrentAsOfEventId;

        public long? FailedOnEventId => readModelInfo.FailedOnEventId;

        public string Error => readModelInfo.Error;

        internal static TimeSpan TimeRemaining(
                TimeSpan timeTakenForProcessedEvents,
                long eventsProcessed,
                long eventsRemaining) =>
            eventsProcessed == 0
                ? (eventsRemaining == 0
                       ? TimeSpan.Zero
                       : TimeSpan.MaxValue)
                : TimeSpan.FromTicks((long) (timeTakenForProcessedEvents.Ticks*(eventsRemaining/(decimal) eventsProcessed)));

        internal static decimal Percent(decimal completed, decimal total) =>
            total == 0
                ? 100
                : completed/total*100;
    }
}