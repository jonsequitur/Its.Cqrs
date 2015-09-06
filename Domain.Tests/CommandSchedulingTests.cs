// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class CommandSchedulingTests
    {
        private CompositeDisposable disposables;
        private Scenario scenario;

        [SetUp]
        public void SetUp()
        {
            // disable authorization
            Command<Order>.AuthorizeDefault = (o, c) => true;
            Command<CustomerAccount>.AuthorizeDefault = (o, c) => true;

            disposables = new CompositeDisposable { VirtualClock.Start() };

            var customerId = Any.Guid();
            scenario = new ScenarioBuilder(c => c.UseInMemoryEventStore()
                                                 .UseInMemoryCommandScheduling())
                .AddEvents(new EventSequence(customerId)
                {
                    new CustomerAccount.UserNameAcquired
                    {
                        UserName = Any.Email()
                    }
                }.ToArray())
                .Prepare();

            disposables.Add(scenario);
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public void When_a_command_is_scheduled_for_later_execution_then_a_CommandScheduled_event_is_added()
        {
            var order = CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));

            var lastEvent = order.PendingEvents.Last();
            lastEvent.Should().BeOfType<CommandScheduled<Order>>();
            lastEvent.As<CommandScheduled<Order>>().Command.Should().BeOfType<Ship>();
        }

        [Test]
        public async Task Scheduled_commands_are_reserialized_and_invoked_by_a_command_scheduler()
        {
            // arrange
            var bus = new FakeEventBus();
            var shipmentId = Any.AlphanumericString(10);
            var repository = new InMemoryEventSourcedRepository<Order>(bus: bus);

            var scheduler = new InMemoryCommandScheduler<Order>(repository);
            bus.Subscribe(scheduler);
            var order = CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            await repository.Save(order);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            //assert 
            order = await repository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
            order.ShipmentId.Should().Be(shipmentId, "Properties should be transferred correctly from the serialized command");
        }

        [Test]
        public async Task InMemoryCommandScheduler_executes_scheduled_commands_immediately_if_no_due_time_is_specified()
        {
            // arrange
            var bus = new InProcessEventBus();
            var repository = new InMemoryEventSourcedRepository<Order>(bus: bus);
            var scheduler = new InMemoryCommandScheduler<Order>(repository);
            bus.Subscribe(scheduler);
            var order = CreateOrder();

            // act
            order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
            await repository.Save(order);

            await scheduler.Done();

            //assert 
            order = await repository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            // arrange
            var order = CreateOrder(customerAccountId: (await scenario.GetLatestAsync<CustomerAccount>()).Id);
            // by the time Ship is applied, it will fail because of the cancellation
            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));
            order.Apply(new Cancel());
            await scenario.SaveAsync(order);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            //assert 
            order = await scenario.GetLatestAsync<Order>();
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.ShipmentCancelled>();
        }

        [Test]
        public async Task When_applying_a_scheduled_command_throws_unexpectedly_then_further_command_scheduling_is_not_interrupted()
        {
            // arrange
            var customerAccountId = (await scenario.GetLatestAsync<CustomerAccount>()).Id;
            var order1 = CreateOrder(customerAccountId: customerAccountId)
                .Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date))
                .Apply(new Cancel());
            await scenario.SaveAsync(order1);
            var order2 = CreateOrder(customerAccountId: customerAccountId)
                .Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));
            await scenario.SaveAsync(order2);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            // assert 
            order1 = await scenario.GetLatestAsync<Order>(order1.Id);
            var lastEvent = order1.Events().Last();
            lastEvent.Should().BeOfType<Order.ShipmentCancelled>();

            order2 = await scenario.GetLatestAsync<Order>(order2.Id);
            lastEvent = order2.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public async Task A_command_can_be_scheduled_against_another_aggregate()
        {
            var order = new Order(
                new CreateOrder(Any.FullName())
                {
                    CustomerId = (await scenario.GetLatestAsync<CustomerAccount>()).Id
                })
                .Apply(new AddItem
                {
                    ProductName = Any.Word(),
                    Price = 12.99m
                })
                .Apply(new Cancel());
            await scenario.SaveAsync(order);

            var customerAccount = await scenario.GetLatestAsync<CustomerAccount>();

            customerAccount.Events()
                           .Last()
                           .Should()
                           .BeOfType<CustomerAccount.OrderCancelationConfirmationEmailSent>();
        }

        [Test]
        public void If_Schedule_is_dependent_on_an_event_with_no_aggregate_id_then_it_throws()
        {
            var scheduler = new ImmediateCommandScheduler<CustomerAccount>(
                new InMemoryEventSourcedRepository<CustomerAccount>(),
                new CommandPreconditionVerifier());

            Action schedule = () => scheduler.Schedule(
                Any.Guid(),
                new SendOrderConfirmationEmail(Any.Word()),
                deliveryDependsOn: new Order.Created
                {
                    AggregateId = Guid.Empty,
                    ETag = Any.Word()
                }).Wait();

            schedule.ShouldThrow<ArgumentException>()
                    .And
                    .Message
                    .Should()
                    .Contain("An AggregateId must be set on the event on which the scheduled command depends.");
        }

        [Test]
        public void If_Schedule_is_dependent_on_an_event_with_no_ETag_then_it_sets_one()
        {
            var scheduler = new ImmediateCommandScheduler<CustomerAccount>(new InMemoryEventSourcedRepository<CustomerAccount>(),
                                                                           new CommandPreconditionVerifier());

            var created = new Order.Created
            {
                AggregateId = Any.Guid(),
                ETag = null
            };

            scheduler.Schedule(
                Any.Guid(),
                new SendOrderConfirmationEmail(Any.Word()),
                deliveryDependsOn: created);

            created.ETag.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_used_to_specify_command_scheduler_behavior_on_schedule()
        {
            var scheduled = false;
            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        scheduled = true;
                    });

            var scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            scheduled.Should().BeTrue();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_used_to_specify_command_scheduler_behavior_on_deliver()
        {
            var delivered = false;
            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .AddToCommandSchedulerPipeline<Order>(
                    deliver: async (cmd, next) => { delivered = true; });

            var scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Deliver(new CommandScheduled<Order>());

            delivered.Should().BeTrue();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_composed_using_several_calls_prior_to_the_scheduler_being_resolved()
        {
            var checkpoints = new List<string>();

            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("two");
                        await next(cmd);
                        checkpoints.Add("three");
                    })
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("one");
                        await next(cmd);
                        checkpoints.Add("four");
                    });

            var scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            checkpoints.Should().BeEquivalentTo(new[] { "one", "two", "three", "four" });
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_composed_using_additional_calls_after_the_scheduler_has_been_resolved()
        {
            var checkpoints = new List<string>();

            var configuration = new Configuration()
                .UseInMemoryEventStore()
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("one");
                        await next(cmd);
                        checkpoints.Add("four");
                    });

            var scheduler = configuration.CommandScheduler<Order>();

            configuration.AddToCommandSchedulerPipeline<Order>(
                schedule: async (cmd, next) =>
                {
                    checkpoints.Add("two");
                    await next(cmd);
                    checkpoints.Add("three");
                });

            scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            checkpoints.Should().BeEquivalentTo(new[] { "one", "two", "three", "four" });
        }

        [Test]
        public async Task A_scheduled_command_is_due_if_no_due_time_is_specified()
        {
            var command = new CommandScheduled<Order>
            {
                Command = new AddItem()
            };

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public async Task A_scheduled_command_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_current_domain_clock()
        {
            var command = new CommandScheduled<Order>
            {
                Command = new AddItem(),
                DueTime = Clock.Now().Subtract(TimeSpan.FromSeconds(1))
            };

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public async Task A_scheduled_command_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_specified_clock()
        {
            var command = new CommandScheduled<Order>
            {
                Command = new AddItem(),
                DueTime = Clock.Now().Add(TimeSpan.FromDays(1))
            };

            command.IsDue(Clock.Create(() => Clock.Now().Add(TimeSpan.FromDays(2))))
                   .Should()
                   .BeTrue();
        }

        [Test]
        public async Task A_scheduled_command_is_not_due_if_a_due_time_is_specified_that_is_later_than_the_current_domain_clock()
        {
            var command = new CommandScheduled<Order>
            {
                Command = new AddItem(),
                DueTime = Clock.Now().Add(TimeSpan.FromSeconds(1))
            };

            command.IsDue()
                   .Should()
                   .BeFalse();
        }

        [Test]
        public async Task A_scheduled_command_is_not_due_if_it_has_already_been_delivered_and_failed()
        {
            var command = new CommandScheduled<Order>
            {
                Command = new AddItem()
            };
            command.Result = new CommandFailed(command);

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public async Task A_scheduled_command_is_not_due_if_it_has_already_been_delivered_and_succeeded()
        {
            var command = new CommandScheduled<Order>
            {
                Command = new AddItem()
            };
            command.Result = new CommandSucceeded(command);

            command.IsDue().Should().BeFalse();
        }

        public static Order CreateOrder(
            DateTimeOffset? deliveryBy = null,
            string customerName = null,
            Guid? orderId = null,
            Guid? customerAccountId = null)
        {
            return new Order(
                new CreateOrder(customerName ?? Any.FullName())
                {
                    AggregateId = orderId ?? Any.Guid(),
                    CustomerId = customerAccountId ?? Any.Guid()
                })
                .Apply(new AddItem
                {
                    Price = 499.99m,
                    ProductName = Any.Words(1, true).Single()
                })
                .Apply(new SpecifyShippingInfo
                {
                    Address = Any.Words(1, true).Single() + " St.",
                    City = "Seattle",
                    StateOrProvince = "WA",
                    Country = "USA",
                    DeliverBy = deliveryBy
                });
        }
    }
}