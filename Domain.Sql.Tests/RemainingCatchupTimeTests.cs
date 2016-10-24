// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
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
            Events.Write(10);
            var eventCount = EventStoreDbContext().DisposeAfter(_ => _.Events.Count());
            var eventsProcessed = 0;
            IEnumerable<EventHandlerProgress> progress = null;

            var projector = CreateProjector(e =>
            {
                if (eventsProcessed == eventCount/2)
                {
                    // the first half of the catchup took 10 minutes
                    VirtualClock.Current.AdvanceBy(10.Minutes());
                    progress = CalculateProgress(() => ReadModelDbContext());
                }
                eventsProcessed++;
            });

            //act
            await RunCatchup(projector,
                startAtEventId: 0,
                batchSize: int.MaxValue);

            // assert
            progress
                .Single(p => p.Name == EventHandler.FullName(projector))
                .TimeRemainingForCatchup
                .Should()
                .BeCloseTo(10.Minutes(), precision: 1000);
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_replay_then_the_remaining_time_is_estimated_correctly()
        {
            //arrange
            Events.Write(10);
            IEnumerable<EventHandlerProgress> progress = null;
            var eventsProcessed = 0;
            var projector = CreateProjector(e =>
            {
                if (eventsProcessed == 15)
                {
                    progress = CalculateProgress(() => ReadModelDbContext());
                }
                VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(1));
                eventsProcessed++;
            });
            await RunCatchup(projector);

            //new set of events come in
            Events.Write(10);

            //act
            await RunCatchup(projector);
            progress.Single(p => p.Name == EventHandler.FullName(projector))
                    .TimeRemainingForCatchup
                    .Should()
                    .Be(TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task If_events_have_been_processed_after_initial_replay_then_the_time_taken_for_initial_replay_is_saved()
        {
            //arrange
            var projector = CreateProjector(e =>
            {
                VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(1));
            });

            //Initial replay
            Events.Write(10);
            await RunCatchup(projector);

            //new set of events come in
            Events.Write(5);

            //act
            await RunCatchup(projector);
            var progress = CalculateProgress(() => ReadModelDbContext());

            //assert
            progress.Single(p => p.Name == EventHandler.FullName(projector))
                    .TimeTakenForInitialCatchup
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

            await RunCatchup(projector);

            //new set of events come in
            Events.Write(5);
            await RunCatchup(projector);

            // act
            var progress = CalculateProgress(() => ReadModelDbContext());

            // assert
            progress.Single(p => p.Name == EventHandler.FullName(projector))
                    .InitialCatchupEvents
                    .Should()
                    .Be(eventCount);
        }

        [Test]
        public async Task If_events_have_been_processed_then_the_correct_number_of_remaining_events_is_returned()
        {
            //arrange
            IEnumerable<EventHandlerProgress> progress = null;
            Events.Write(5);

            var eventsProcessed = 0;
            var projector = CreateProjector(e =>
            {
                if (eventsProcessed == 4)
                {
                    progress = CalculateProgress(() => ReadModelDbContext());
                }
                eventsProcessed++;
            });

            //act
            await RunCatchup(projector);

            //assert
            progress.Single(p => p.Name == EventHandler.FullName(projector))
                    .EventsRemaining
                    .Should()
                    .Be(1);
        }

        [Test]
        public async Task If_all_events_have_been_processed_then_the_remaining_time_is_zero()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector();
            await RunCatchup(projector);

            //act
            var progress = CalculateProgress(() => ReadModelDbContext());

            //assert
            progress.Single(p => p.Name == EventHandler.FullName(projector))
                    .TimeRemainingForCatchup
                    .Should()
                    .Be(TimeSpan.FromMinutes(0));
        }

        [Test]
        public async Task If_all_events_have_been_processed_then_the_percentage_completed_is_100()
        {
            //arrange
            Events.Write(5);
            var projector = CreateProjector();
            await RunCatchup(projector);

            //act
            var progress = CalculateProgress(() => ReadModelDbContext());

            //assert
            progress.Single(p => p.Name == EventHandler.FullName(projector))
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

            await RunCatchup(batchSize: 3, projector: projector);

            //act
            var progress = CalculateProgress(() => ReadModelDbContext());

            //assert
            var handlerProgress = progress.Single(p => p.Name == EventHandler.FullName(projector));

            handlerProgress
                .InitialCatchupEvents
                .Should()
                .Be(eventCount);
        }

        private IUpdateProjectionWhen<IEvent> CreateProjector(
                Action<IEvent> action = null) =>
            Projector.Create(action ?? (_ => { })).Named(Any.CamelCaseName(6));

        private async Task RunCatchup(
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
                await catchup.Run();
            }
        }
    }
}