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

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Catchups")]
    [TestFixture]
    [UseSqlEventStore]
    public class RemainingCatchupTimeTests : EventStoreDbTest
    {
        [Test]
        public async Task During_initial_replay_the_remaining_time_is_estimated_correctly()
        {
            //arrange
            Events.Write(20);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());

            var eventsProcessed = 0;
            EventHandlerProgress progress = null;
            var batchSize = eventCount/10 + 1;

            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector(e =>
            {
                eventsProcessed++;
                if (eventsProcessed == batchSize)
                {
                    VirtualClock.Current.AdvanceBy(1.Minutes());
                    progress = CalculateProgressFor(projector);
                }
            });

            //act
            await RunCatchupSingleBatch(projector,
                startAtEventId: 0,
                batchSize: batchSize);

            Console.WriteLine(new { eventCount, eventsProcessed, batchSize });

            // assert
            progress.TimeRemainingForCatchup
                    .Should()
                    .BeCloseTo(10.Minutes(), precision: 30000 /* milliseconds */);
        }

        [Test]
        public async Task During_initial_replay_the_percent_completed_is_estimated_correctly()
        {
            //arrange
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());
            var eventsProcessed = 0;
            EventHandlerProgress progress = null;

            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector(e =>
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

            Console.WriteLine(new { eventCount, eventsProcessed, batchSize });

            // assert
            progress.PercentageCompleted
                    .Should()
                    .BeInRange(10, 11);
        }

        [Test]
        public async Task TimeTakenForInitialCatchup_should_be_null_when_initial_catchup_is_not_done()
        {
            // arrange
            Events.Write(10);

            var projector = CreateProjector();

            // act
            await RunCatchupSingleBatch(projector,
                startAtEventId: 0,
                batchSize: 5);

            var progress = CalculateProgressFor(projector);

            //assert
            progress.TimeTakenForInitialCatchup.Should().NotHaveValue();
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_replay_then_the_remaining_time_is_estimated_correctly()
        {
            //arrange
            Events.Write(10);
            EventHandlerProgress progress = null;
            var eventsProcessed = 0;
            IUpdateProjectionWhen<IEvent> projector = null;

            projector = CreateProjector(e =>
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

            progress.TimeRemainingForCatchup
                    .Should()
                    .Be(TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_replay_then_the_time_taken_for_initial_replay_is_saved()
        {
            //arrange
            var projector = CreateProjector(e => { VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(1)); });

            //Initial replay
            Events.Write(10);
            await RunCatchupSingleBatch(projector);

            //new set of events come in
            Events.Write(5);

            //act
            await RunCatchupSingleBatch(projector);
            var progress = CalculateProgressFor(projector);

            //assert
            progress.TimeTakenForInitialCatchup
                    .Should()
                    .Be(TimeSpan.FromSeconds(9));
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_replay_then_the_number_of_events_for_initial_replay_is_saved()
        {
            // arrange
            var projector = CreateProjector();
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());

            await RunCatchupSingleBatch(projector);

            //new set of events come in
            Events.Write(5);
            await RunCatchupSingleBatch(projector);

            // act
            var progress = CalculateProgressFor(projector);

            // assert
            progress.InitialCatchupEvents
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
            projector = CreateProjector(e =>
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
            progress.EventsRemainingInBatch
                    .Should()
                    .Be(1);
        }

        [Test]
        public async Task If_all_events_have_been_processed_then_the_remaining_time_is_zero()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector();
            await RunCatchupSingleBatch(projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress.TimeRemainingForCatchup
                    .Should()
                    .Be(TimeSpan.FromMinutes(0));
        }

        [Test]
        public async Task If_all_events_have_been_processed_out_of_the_initial_catchup_then_the_percentage_completed_is_100()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector();
            await RunCatchupSingleBatch(projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress
                .PercentageCompleted
                .Should()
                .Be(100);
        }

        [Test]
        public async Task If_all_events_have_been_processed_out_of_a_subsequent_catchup_then_the_percentage_completed_is_100()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector();
            await RunCatchupSingleBatch(projector);
            CalculateProgressFor(projector);
            Events.Write(5);
            await RunCatchupSingleBatch(projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress
                .PercentageCompleted
                .Should()
                .Be(100);
        }

        [Test]
        public async Task InitialCatchupEvents_is_not_limited_by_batch_size()
        {
            // arrange
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());
            var projector = CreateProjector();

            await RunCatchupSingleBatch(batchSize: 3, projector: projector);

            //act
            var progress = CalculateProgressFor(projector);

            //assert
            progress
                .InitialCatchupEvents
                .Should()
                .Be(eventCount);
        }

        private IUpdateProjectionWhen<IEvent> CreateProjector(
                Action<IEvent> action = null) =>
            Projector.Create(action ?? (_ => { })).Named(Any.CamelCaseName(6));

        private static EventHandlerProgress CalculateProgressFor(
            IUpdateProjectionWhen<IEvent> projector)
        {
            var projectorName = EventHandler.FullName(projector);

            var progress = CalculateProgress(
                    () => ReadModelDbContext(),
                    filter: i => i.Name == projectorName)
                .Single();

            Console.WriteLine(progress.ToJson(Formatting.Indented));

            return progress;
        }

        private async Task RunCatchupSingleBatch(
            IUpdateProjectionWhen<IEvent> projector,
            int batchSize = 100,
            long? startAtEventId = null)
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
    }
}