using System;
using System.Collections.Generic;
using FluentAssertions;
using Its.Log.Instrumentation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlCommandSchedulerTests_Legacy : SqlCommandSchedulerTests
    {
        private SqlCommandScheduler sqlCommandScheduler;

        [Test]
        public void All_commands_can_be_configured_for_SQL_scheduling_using_ScheduleCommandsUsingSqlScheduler()
        {
            var eventBus = new FakeEventBus();

            eventBus.Subscribe(new SqlCommandScheduler(new Configuration().UseSqlEventStore()));

            eventBus.SubscribedEventTypes()
                    .Should()
                    .Contain(new[]
                    {
                        typeof (IScheduledCommand<Order>),
                        typeof (IScheduledCommand<CustomerAccount>)
                    });
        }

        [Test]
        public void Once_SqlCommandScheduler_is_resolved_from_the_Configuration_then_aggregate_specific_command_schedulers_are_registered()
        {
            var configuration = new Configuration()
                .UseDependency<GetClockName>(c => e => SqlCommandScheduler.DefaultClockName)
                .UseInMemoryEventStore();
            configuration.Container.Resolve<SqlCommandScheduler>();

            var commandScheduler = configuration.Container.Resolve<ICommandScheduler<Order>>();

            commandScheduler.Should()
                            .NotBeNull();
            configuration.Container
                         .Resolve<ICommandScheduler<Order>>()
                         .Should()
                         .BeSameAs(commandScheduler);
        }

        [Test]
        public async Task Activity_is_notified_when_a_command_is_scheduled()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            var activity = new List<ICommandSchedulerActivity>();

            using (Configuration.Current
                                .Container
                                .Resolve<SqlCommandScheduler>()
                                .Activity
                                .Subscribe(activity.Add))
            {
                // act
                order.Apply(new ShipOn(Clock.Now().Add(TimeSpan.FromDays(2))));
                await orderRepository.Save(order);

                //assert 
                activity.Should()
                        .ContainSingle(a => a.ScheduledCommand.AggregateId == order.Id &&
                                            a is CommandScheduled);
            }
        }

        [Test]
        public async Task Activity_is_notified_when_a_command_is_delivered_immediately()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            var activity = new List<ICommandSchedulerActivity>();

            using (Configuration.Current
                                .Container
                                .Resolve<SqlCommandScheduler>()
                                .Activity
                                .Subscribe(a => activity.Add(a)))
            {
                // act
                order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
                await orderRepository.Save(order);

                await SchedulerWorkComplete();

                //assert 
                activity.Should()
                        .ContainSingle(a => a.ScheduledCommand.AggregateId == order.Id &&
                                            a is CommandSucceeded);
            }
        }

        [Test]
        public async Task SqlCommandScheduler_Activity_is_notified_when_a_command_is_delivered_via_Trigger()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            var activity = new List<ICommandSchedulerActivity>();

            order.Apply(new ShipOn(Clock.Now().Add(TimeSpan.FromDays(2))));
            await orderRepository.Save(order);

            using (sqlCommandScheduler.Activity.Subscribe(activity.Add))
            {
                // act
                await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(3));

                //assert 
                activity.Should()
                        .ContainSingle(a => a.ScheduledCommand.AggregateId == order.Id &&
                                            a is CommandSucceeded);
            }
        }

        [Test]
        public async Task A_command_is_not_marked_as_applied_if_no_handler_is_registered()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(10)
                });
            await orderRepository.Save(order);

            // act
            var schedulerWithNoHandlers = new SqlCommandScheduler(
                new Configuration().UseSqlEventStore());
            await schedulerWithNoHandlers.AdvanceClock(clockName, @by: TimeSpan.FromDays(20));

            // assert
            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands.Single(c => c.AggregateId == order.Id)
                  .AppliedTime
                  .Should()
                  .BeNull();
            }
        }

        protected override void ConfigureScheduler(Configuration configuration)
        {
            configuration.UseSqlCommandScheduling();
            sqlCommandScheduler = configuration.SqlCommandScheduler();
            sqlCommandScheduler.GetClockName = e => clockName;
            clockTrigger = sqlCommandScheduler;
            clockRepository = sqlCommandScheduler;

            disposables.Add(sqlCommandScheduler.Activity.Subscribe(s => Console.WriteLine("SqlCommandScheduler: " + s.ToLogString())));
        }

        protected override async Task SchedulerWorkComplete()
        {
            await sqlCommandScheduler.Done(clockName);
        }
    }
}