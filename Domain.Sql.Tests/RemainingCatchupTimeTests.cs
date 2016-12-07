// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;
using NUnit.Framework;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;
using static Microsoft.Its.Domain.Sql.EventHandlerProgressCalculator;
using static Microsoft.Its.Domain.Sql.EventHandlerProgress;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Catchups")]
    [TestFixture]
    [UseSqlEventStore]
    public class RemainingCatchupTimeTests : EventStoreDbTest
    {
        [Test]
        public async Task During_initial_catchup_the_remaining_time_is_estimated_correctly()
        {
            //arrange
            Events.Write(20, randomEventTypes: true);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());

            var eventsProcessed = 0;
            EventHandlerProgress progress = null;

            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector<IEvent>(e =>
            {
                eventsProcessed++;
                if (eventsProcessed == eventCount/100)
                {
                    VirtualClock.Current.AdvanceBy(1.Minutes());
                    progress = CalculateProgressFor(projector);
                }
            });
             
            //act
            var batchSize = (int) ((eventCount/100) * 1.1);
            await RunCatchupSingleBatch(projector,
                startAtEventId: 0,
                batchSize: batchSize);

            // assert
            var expectedTimeRemaining = TimeRemaining(
                1.Minutes(),
                eventsProcessed: eventCount/100,
                eventsRemaining: eventCount - (eventCount/100) );

            progress.InitialCatchupTimeRemaining
                    .Should()
                    .BeCloseTo(expectedTimeRemaining);
        }

        [Test]
        public async Task During_initial_catchup_the_remaining_time_is_estimated_correctly_across_multiple_batches()
        {
            //arrange
            Events.Write(20, randomEventTypes: true);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());

            var eventsProcessed = 0;
            EventHandlerProgress progress = null;

            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector<IEvent>(e =>
            {
                eventsProcessed++;
                if (eventsProcessed == eventCount/100)
                {
                    VirtualClock.Current.AdvanceBy(1.Minutes());
                    progress = CalculateProgressFor(projector);
                }
            });

            //act
            for (var i = 0; i < 13; i++)
            {
                await RunCatchupSingleBatch(projector,
                    startAtEventId: 0,
                    batchSize: eventCount/1000);
            }

            // assert
            var expectedTimeRemaining = TimeRemaining(
                1.Minutes(),
                eventsProcessed: eventCount/100,
                eventsRemaining: eventCount - (eventCount/100));

            progress.InitialCatchupTimeRemaining
                    .Should()
                    .BeCloseTo(expectedTimeRemaining);
        }

        [Test]
        public async Task During_initial_catchup_the_percent_completed_is_estimated_correctly()
        {
            //arrange
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());
            var eventsProcessed = 0;
            EventHandlerProgress progress = null;

            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector<IEvent>(e =>
            {
                if (eventsProcessed == eventCount/10 + 1)
                {
                    VirtualClock.Current.AdvanceBy(10.Minutes());
                    progress = CalculateProgressFor(projector);
                }
                eventsProcessed++;
            });

            //act
            // catch up enough events to hit the if block in the projector
            var batchSize = eventCount/8;

            await RunCatchupSingleBatch(projector,
                startAtEventId: 0,
                batchSize: batchSize);

            // assert
            progress.InitialCatchupPercentageCompleted
                    .Should()
                    .BeInRange(10, 11);
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_catchup_then_the_remaining_time_is_estimated_correctly()
        {
            //arrange
            Events.Write(10);
            EventHandlerProgress progress = null;
            var eventsProcessed = 0;
            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector<IEvent>(e =>
            {
                if (eventsProcessed == 15)
                {
                    progress = CalculateProgressFor(projector);
                }
                VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(1));
                eventsProcessed++;
            });
            await RunCatchupSingleBatch(projector);

            //new set of events come in
            Events.Write(10);

            //act
            await RunCatchupSingleBatch(projector);

            progress.BatchTimeRemaining
                    .Should()
                    .Be(TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_catchup_then_the_time_taken_for_initial_catchup_is_saved()
        {
            //arrange
            var projector = CreateProjector<IEvent>(e => { VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(1)); });

            //Initial catchup
            Events.Write(10);
            await RunCatchupSingleBatch(projector);

            //new set of events come in
            Events.Write(5);

            //act
            await RunCatchupSingleBatch(projector);
            var progress = CalculateProgressFor(projector);

            //assert
            progress.InitialCatchupTimeElapsed
                    .Should()
                    .Be(TimeSpan.FromSeconds(9));
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_catchup_then_the_number_of_events_for_initial_catchup_is_saved()
        {
            // arrange
            var projector = CreateProjector<IEvent>();
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());

            await RunCatchupSingleBatch(projector);

            //new set of events come in
            Events.Write(5);
            await RunCatchupSingleBatch(projector);

            // act
            var progress = CalculateProgressFor(projector);

            // assert
            progress.InitialCatchupTotalEvents
                    .Should()
                    .Be(eventCount);
        }

        [Test]
        public async Task If_events_have_been_processed_then_the_correct_number_of_remaining_events_is_returned()
        {
            //arrange
            EventHandlerProgress progress = null;
            Events.Write(5);

            var eventsProcessed = 0;
            IUpdateProjectionWhen<IEvent> projector = null;
            projector = CreateProjector<IEvent>(e =>
            {
                if (eventsProcessed == 4)
                {
                    progress = CalculateProgressFor(projector);
                }
                eventsProcessed++;
            });

            //act
            await RunCatchupSingleBatch(projector);

            //assert
            progress.BatchRemainingEvents
                    .Should()
                    .Be(1);
        }

        [Test]
        public async Task If_all_events_have_been_processed_then_the_remaining_time_is_zero()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector<IEvent>();
            await RunCatchupSingleBatch(projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress.InitialCatchupTimeRemaining
                    .Should()
                    .Be(TimeSpan.FromMinutes(0));
        }

        [Test]
        public async Task If_all_events_have_been_processed_out_of_the_initial_catchup_then_the_percentage_completed_is_100()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector<IEvent>();
            await RunCatchupSingleBatch(projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress
                .InitialCatchupPercentageCompleted
                .Should()
                .Be(100);
        }

        [Test]
        public async Task If_all_events_have_been_processed_out_of_a_batch_then_the_percentage_completed_is_100()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector<IEvent>();
            await RunCatchupSingleBatch(projector);
            CalculateProgressFor(projector);
            Events.Write(5);
            await RunCatchupSingleBatch(projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress
                .BatchPercentageCompleted
                .Should()
                .Be(100);
        }

        [Test]
        public async Task InitialCatchupEvents_is_not_limited_by_batch_size()
        {
            // arrange
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());
            var projector = CreateProjector<IEvent>();

            await RunCatchupSingleBatch(batchSize: 3, projector: projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress
                .InitialCatchupTotalEvents
                .Should()
                .Be(eventCount);
        }

        [Test]
        public async Task TimeRemaining_correctly_calculates_time_remaining_based_on_events_processed_and_events_remaining()
        {
            TimeRemaining(1.Minutes(),
                    1,
                    100)
                .Should()
                .Be(100.Minutes());

            TimeRemaining(123.Minutes(),
                    123,
                    1)
                .Should()
                .BeCloseTo(1.Minutes(), precision: 1);

            TimeRemaining(23.Minutes(),
                    23,
                    1)
                .Should()
                .BeCloseTo(1.Minutes(), precision: 1);
        }

        [Test]
        public async Task When_initial_catchup_has_not_completed_then_time_elapsed_increases_as_time_passes()
        {
            var startTime = DateTimeOffset.Parse("2016-01-01 12:00am +00:00");

            var readModelInfo = new ReadModelInfo
            {
                InitialCatchupStartTime = startTime,
                InitialCatchupEndTime = null,
                BatchRemainingEvents = 1000
            };

            new EventHandlerProgress(readModelInfo, startTime)
                .InitialCatchupTimeElapsed
                .Should()
                .Be(0.Minutes());

            new EventHandlerProgress(readModelInfo, startTime.AddHours(1))
                .InitialCatchupTimeElapsed
                .Should()
                .Be(1.Hours());
        }

        [Test]
        public async Task When_initial_catchup_has_completed_then_time_elapsed_does_not_increases_as_time_passes()
        {
            var startTime = DateTimeOffset.Parse("2016-01-01 12:00am +00:00");

            var readModelInfo = new ReadModelInfo
            {
                InitialCatchupStartTime = startTime,
                InitialCatchupEndTime = startTime.AddHours(52),
                BatchRemainingEvents = 1000
            };

            new EventHandlerProgress(readModelInfo, startTime.AddDays(4))
                .InitialCatchupTimeElapsed
                .Should()
                .Be(52.Hours());
        }

        private IUpdateProjectionWhen<T> CreateProjector<T>(
            Action<T> action = null)
            where T : IEvent =>
            Projector.Create(action ?? (_ => { })).Named(Any.CamelCaseName(6));

        private static EventHandlerProgress CalculateProgressFor<T>(
            IUpdateProjectionWhen<T> projector) where T : IEvent
        {
            var projectorName = EventHandler.FullName(projector);

            var progress = CalculateProgress(
                    () => ReadModelDbContext(),
                    filter: i => i.Name == projectorName)
                .Single();

            Console.WriteLine(progress.ToJson(Formatting.Indented));

            return progress;
        }

        private async Task RunCatchupSingleBatch<T>(
            IUpdateProjectionWhen<T> projector,
            int batchSize = 100,
            long? startAtEventId = null)
            where T : IEvent
        {
            if (projector == null)
            {
                throw new ArgumentNullException(nameof(projector));
            }

            using (var catchup = CreateReadModelCatchup(
                batchSize: batchSize,
                projectors: projector,
                startAtEventId: startAtEventId))
            {
                await catchup.SingleBatchAsync();
            }
        }

        private async Task RunCatchupSingleBatch(
            int batchSize,
            long? startAtEventId,
            params object[] projectors)
        {
            using (var catchup = CreateReadModelCatchup(
                batchSize: batchSize,
                projectors: projectors,
                startAtEventId: startAtEventId))
            {
                await catchup.SingleBatchAsync();
            }
        }
    }
}