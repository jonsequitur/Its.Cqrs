// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class VirtualClockCommandSchedulingTests
    {
        [SetUp]
        public void SetUp()
        {
              Command<Order>.AuthorizeDefault = (order, command) => true;
              CommandSchedulerDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";
        }

        [TearDown]
        public void TearDown()
        {
            Clock.Reset();   
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