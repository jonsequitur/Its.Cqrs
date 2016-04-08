// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
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

        protected abstract Configuration GetConfiguration();
    }
}