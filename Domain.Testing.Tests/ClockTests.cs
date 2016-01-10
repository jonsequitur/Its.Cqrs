// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ClockTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = (order, command) => true;
            Clock.Reset();
            CommandSchedulerDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";
        }

        [Test]
        public void VirtualClock_Start_can_be_used_to_specify_a_virtual_time_that_Clock_Now_will_return()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
                Clock.Now().Should().Be(time);
            }
        }

        [Test]
        public void When_VirtualClock_is_disposed_then_Clock_Current_is_restored_to_the_system_clock()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
            }

            var now = Clock.Now();
            now.Should().BeInRange(now, DateTimeOffset.Now.AddMilliseconds(10));
        }

        [Test]
        public void VirtualClock_cannot_go_back_in_time_using_AdvanceTo()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
                Action goBackInTime = () => VirtualClock.Current.AdvanceTo(time + TimeSpan.FromSeconds(-1));
                goBackInTime.ShouldThrow<ArgumentException>();
            }
        }

        [Test]
        public void VirtualClock_cannot_go_back_in_time_using_AdvanceBy()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
                Action goBackInTime = () => VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(-1));
                goBackInTime.ShouldThrow<ArgumentException>();
            }
        }

        [Test]
        public void When_VirtualClock_Start_is_called_while_a_VirtualClock_is_already_in_use_it_throws()
        {
            VirtualClock.Start();
            Action startAgain = () => VirtualClock.Start();
            startAgain.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public async Task Advancing_the_clock_blocks_until_triggered_commands_on_the_command_scheduler_are_completed()
        {
            VirtualClock.Start();

            var configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .UseInMemoryEventStore();

            var scheduler = configuration.CommandScheduler<Order>();
            var repository = configuration.Repository<Order>();

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
        public async Task When_using_legacy_SQL_command_scheduling_then_advancing_the_clock_blocks_until_triggered_commands_are_completed()
        {
            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .UseSqlCommandScheduling()
                .TriggerSqlCommandSchedulerWithVirtualClock();

            configuration.SqlCommandScheduler()
                         .GetClockName = e => Any.CamelCaseName();

            await ScheduleCommandAndAdvanceClock(configuration);
        }

        [Test]
        public async Task When_using_pipelined_SQL_command_scheduling_then_advancing_the_clock_blocks_until_triggered_commands_are_completed()
        {
            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .UseDependency<GetClockName>(c => e => Any.CamelCaseName())
                .UseSqlStorageForScheduledCommands();

            await ScheduleCommandAndAdvanceClock(configuration);
        }

        private static async Task ScheduleCommandAndAdvanceClock(Configuration configuration)
        {
            using (ConfigurationContext.Establish(configuration)) 
            using (VirtualClock.Start())
            {
                var scheduler = configuration.CommandScheduler<Order>();
                var repository = configuration.Repository<Order>();

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
        }
    }
}