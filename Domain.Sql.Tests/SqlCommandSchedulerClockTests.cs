// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [NUnit.Framework.Category("Command scheduling")]
    [TestFixture]
    [UseSqlStorageForScheduledCommands]
    [UseInMemoryCommandTargetStore]
    [DisableCommandAuthorization]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerClockTests : SchedulerClockTests
    {
        protected Task<SchedulerAdvancedResult> AdvanceClock(TimeSpan by, string clockName) =>
            Configuration
                .Current
                .SchedulerClockTrigger()
                .AdvanceClock(
                    clockName: clockName,
                    by: by);

        protected Task<SchedulerAdvancedResult> AdvanceClock(DateTimeOffset to, string clockName) =>
            Configuration
                .Current
                .SchedulerClockTrigger()
                .AdvanceClock(
                    clockName: clockName,
                    to: to);

        protected static CommandScheduler.Clock CreateClock(string named, DateTimeOffset startTime) =>
            (CommandScheduler.Clock) Configuration
                                         .Current
                                         .SchedulerClockRepository()
                                         .CreateClock(named, startTime);

        [Test]
        public override void A_clock_cannot_be_moved_to_a_prior_time()
        {
            // arrange
            var name = Any.AlphanumericString(8, 8);

            CreateClock(name, DateTimeOffset.UtcNow);

            // act
            Action moveBackwards = () => AdvanceClock(DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(5)), name).Wait();

            // assert
            moveBackwards.ShouldThrow<InvalidOperationException>()
                .And
                .Message
                .Should()
                .Contain("A clock cannot be moved backward");
        }

        [Test]
        public override void Two_clocks_cannot_be_created_having_the_same_name()
        {
            var name = Any.CamelCaseName();

            CreateClock(name, DateTimeOffset.UtcNow);

            Action createAgain = () =>
                                 CreateClock(name, DateTimeOffset.UtcNow.AddDays(1));

            createAgain.ShouldThrow<ConcurrencyException>()
                .And
                .Message
                .Should()
                .Contain($"A clock named '{name}' already exists");
        }

        [Test]
        public async Task When_a_clock_is_advanced_then_unassociated_commands_are_not_triggered()
        {
            // arrange
            var clockOne = CreateClock(Any.CamelCaseName(), Clock.Now());
            var clockTwo = CreateClock(Any.CamelCaseName(), Clock.Now());

            var deliveryAttempts = new ConcurrentBag<IScheduledCommand>();

            Configuration.Current.TraceScheduledCommands(onDelivering: command => { deliveryAttempts.Add(command); });

            var scheduler = Configuration.Current.CommandScheduler<NonEventSourcedCommandTarget>();

            await scheduler
                .Schedule(
                    new CreateCommandTarget(Any.CamelCaseName()),
                    Clock.Now().AddDays(1),
                    clock: clockOne);
            await scheduler
                .Schedule(
                    new CreateCommandTarget(Any.CamelCaseName()),
                    Clock.Now().AddDays(1),
                    clock: clockTwo);

            // act
            await AdvanceClock(TimeSpan.FromDays(2), clockOne.Name);

            //assert 
            deliveryAttempts
                .Should().HaveCount(1)
                .And
                .OnlyContain(c => ((CommandScheduler.Clock) c.Clock).Name == clockOne.Name);
        }
    }
}