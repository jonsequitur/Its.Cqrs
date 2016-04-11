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

namespace Microsoft.Its.Domain.Testing.Tests
{
    public abstract class VirtualClockCommandSchedulingTests
    {
        [Test]
        public async Task Advancing_the_clock_blocks_until_triggered_commands_on_the_command_scheduler_are_completed()
        {
            VirtualClock.Start();

            var scheduler = Configuration.Current.CommandScheduler<Order>();
            var repository = Configuration.Current.Repository<Order>();

            var aggregateId = Any.Guid();
            await scheduler.Schedule(new CommandScheduled<Order>
            {
                Command = new CreateOrder(Any.FullName())
                {
                    AggregateId = aggregateId
                },
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
                Command = new CreateOrder(Any.FullName())
                {
                    AggregateId = aggregateId
                },
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
            var target = new CommandTarget(Any.CamelCaseName())
            {
                OnEnactCommand = async (commandTarget, command) =>
                {
                    await Task.Yield();

                    
                }
            };
            var store = Configuration.Current.Store<CommandTarget>();
            await store.Put(target);

            // act
            await Configuration.Current
                .CommandScheduler<CommandTarget>()
                .Schedule(target.Id,
                                     new TestCommand(isValid: false),
                                     Clock.Now().AddMinutes(2));

            await 

            // FIX (When_a_command_is_delivered_and_throws_during_clock_advance_then_other_commands_are_still_delivered) write test
            Assert.Fail("Test not written yet.");
        }

        protected abstract Configuration GetConfiguration();
    }
}
