// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Tests.NonEventSourcedCommandTarget;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public abstract class VirtualClockCommandSchedulingTests
    {
        protected VirtualClockCommandSchedulingTests()
        {
            Command<NonEventSourcedCommandTarget>.AuthorizeDefault = (target, command) => true;
        }

        [Test]
        public async Task Advancing_the_clock_blocks_until_triggered_commands_on_the_command_scheduler_are_completed()
        {
            VirtualClock.Start();

            var scheduler = Configuration.Current.CommandScheduler<Order>();
            var repository = Configuration.Current.Repository<Order>();

            var aggregateId = Any.Guid();
            await scheduler.Schedule(new CommandScheduled<Order>
            {
                Command = new CreateOrder(aggregateId, Any.FullName()),
                DueTime = Clock.Now().AddHours(1),
                AggregateId = aggregateId
            });

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            var order = await repository.GetLatest(aggregateId);

            order.Should().NotBeNull();
        }

        [Test]
        public async Task When_using_pipelined_SQL_command_scheduling_then_advancing_the_clock_blocks_until_triggered_commands_are_completed()
        {
            VirtualClock.Start();

            var scheduler = Configuration.Current.CommandScheduler<Order>();
            var repository = Configuration.Current.Repository<Order>();

            var aggregateId = Any.Guid();

            await scheduler.Schedule(new CommandScheduled<Order>
            {
                Command = new CreateOrder(aggregateId, Any.FullName()),
                DueTime = Clock.Now().AddHours(1),
                AggregateId = aggregateId
            });

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            var order = await repository.GetLatest(aggregateId);

            order.Should().NotBeNull();
        }

        [Test]
        public async Task When_the_clock_is_advanced_then_commands_are_delivered_in_the_expected_order()
        {
            // arrange
            VirtualClock.Start(DateTimeOffset.Parse("2016-04-08 12:00:00 PM"));

            var aggregate = new CommandSchedulerTestAggregate(Any.Guid());
            var commandsDelivered = new List<IScheduledCommand<CommandSchedulerTestAggregate>>();
            var configuration = Configuration.Current
                                             .TraceScheduledCommands(onDelivering: c => commandsDelivered.Add((IScheduledCommand<CommandSchedulerTestAggregate>) c));

            await configuration.Repository<CommandSchedulerTestAggregate>().Save(aggregate);

            var firstCommandSchedulesSecond = new CommandSchedulerTestAggregate.CommandThatSchedulesAnotherCommand
            {
                NextCommand = new CommandSchedulerTestAggregate.Command
                {
                    ETag = "second"
                },
                NextCommandAggregateId = aggregate.Id,
                NextCommandDueTime = Clock.Now().AddHours(1),
                ETag = "first"
            };

            var thirdCommand = new CommandSchedulerTestAggregate.Command
            {
                ETag = "third"
            };

            var scheduler = configuration.CommandScheduler<CommandSchedulerTestAggregate>();
            await scheduler.Schedule(aggregate.Id,
                                     firstCommandSchedulesSecond,
                                     Clock.Now().AddMinutes(1));

            await scheduler.Schedule(aggregate.Id,
                                     thirdCommand,
                                     Clock.Now().AddDays(1));

            // act
            VirtualClock.Current.AdvanceBy(1.Days().And(1.Seconds()));

            // assert
            Console.WriteLine(commandsDelivered.Select(c => c.DueTime).ToLogString());

            commandsDelivered
                .Select(c => c.Command.ETag)
                .Should()
                .ContainInOrder("first", "second", "third");
        }

        [Test]
        public async Task When_a_command_is_delivered_and_throws_during_clock_advance_then_other_commands_are_still_delivered()
        {
            // arrange
            var target = new NonEventSourcedCommandTarget(Any.CamelCaseName())
            {
                OnEnactCommand = async (commandTarget, command) =>
                {
                    await Task.Yield();

                    if (command.ETag == "first")
                    {
                        throw new Exception("oops!");
                    }
                }
            };
            var store = Configuration.Current.Store<NonEventSourcedCommandTarget>();
            await store.Put(target);
            VirtualClock.Start();

            // act
            var scheduler = Configuration.Current.CommandScheduler<NonEventSourcedCommandTarget>();

            await scheduler.Schedule(target.Id,
                                     new TestCommand(etag: "first"),
                                     Clock.Now().AddMinutes(1));

            await scheduler.Schedule(target.Id,
                                     new TestCommand(etag: "second"),
                                     Clock.Now().AddMinutes(2));

            VirtualClock.Current.AdvanceBy(1.Hours());

            // assert
            target = await store.Get(target.Id);

            target.CommandsEnacted
                  .Select(c => c.ETag)
                  .Should()
                  .Contain(etag => etag == "second");
        }

        [Test]
        public async Task When_a_command_fails_and_is_scheduled_for_retry_then_advancing_the_virtual_clock_triggers_redelivery()
        {
            VirtualClock.Start();

            var target = new NonEventSourcedCommandTarget(Any.CamelCaseName())
            {
                IsValid = false
            };
            var retries = 0;

            var configuration = Configuration.Current;
        
            await configuration.Store<NonEventSourcedCommandTarget>().Put(target);

            configuration.UseCommandHandler<NonEventSourcedCommandTarget, TestCommand>(
                enactCommand: async (_, __) => { },
                handleScheduledCommandException: async (_, command) =>
                {
                    if (command.NumberOfPreviousAttempts >= 12)
                    {
                        command.Cancel();
                        return;
                    }
                    retries++;
                    command.Retry(1.Hours());
                });

            var scheduler = configuration.CommandScheduler<NonEventSourcedCommandTarget>();

            await scheduler.Schedule(target.Id, 
                new TestCommand(), 
                dueTime: Clock.Now().AddHours(1));

            VirtualClock.Current.AdvanceBy(1.Days());

            retries.Should().Be(12);
        }

        [Test]
        public async Task When_the_VirtualClock_is_advanced_past_a_commands_due_time_then_in_EnactCommand_ClockNow_returns_the_commands_due_time()
        {
            var dueTime = DateTimeOffset.Parse("2019-09-01 +00:00");

            VirtualClock.Start(DateTimeOffset.Parse("2019-01-01 +00:00"));

            var target = new NonEventSourcedCommandTarget(Any.CamelCaseName());
            var configuration = Configuration.Current;
            await configuration.Store<NonEventSourcedCommandTarget>().Put(target);

            var clockNowAtCommandDeliveryTime = default(DateTimeOffset);

            configuration.UseCommandHandler<NonEventSourcedCommandTarget, TestCommand>(
                enactCommand: async (_, __) =>
                {
                    clockNowAtCommandDeliveryTime = Clock.Now();
                });

            var scheduler = configuration.CommandScheduler<NonEventSourcedCommandTarget>();

            await scheduler.Schedule(target.Id,
                new TestCommand(),
                dueTime: dueTime);

            VirtualClock.Current.AdvanceBy(365.Days());

            clockNowAtCommandDeliveryTime.Should().Be(dueTime);
        }

        protected abstract Configuration GetConfiguration();
    }
}
