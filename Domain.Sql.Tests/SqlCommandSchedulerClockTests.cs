// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Tests.CurrentConfiguration;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [NUnit.Framework.Category("Command scheduling")]
    [TestFixture]
    [UseSqlStorageForScheduledCommands]
    [UseSqlEventStore]
    [UseInMemoryCommandTargetStore]
    [DisableCommandAuthorization]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerClockTests : SchedulerClockTests
    {
        protected static CommandScheduler.Clock CreateClock(string named, DateTimeOffset startTime) =>
            (CommandScheduler.Clock) Configuration
                                         .Current
                                         .SchedulerClockRepository()
                                         .CreateClock(named, startTime);

        protected int GetScheduledCommandNumberOfAttempts(Guid aggregateId)
        {
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                return db.ScheduledCommands
                    .SingleOrDefault(c => c.AggregateId == aggregateId)
                    .IfNotNull()
                    .Then(c => c.Attempts)
                    .ElseDefault();
            }
        }

        private void TriggerConcurrencyExceptionOnOrderCommands(Guid orderId)
        {
            Func<EventStoreDbContext> eventStoreContext = () =>
            {
                // quick, add a new event in order to trigger a concurrency exception at the moment the scheduler tries to apply the command
                var repository = new SqlEventSourcedRepository<Order>();
                var o = repository.GetLatest(orderId).Result;
                o.Apply(new Annotate<Order>("triggering a concurrency exception", Any.Guid().ToString()));
                repository.Save(o).Wait();

                return EventStoreDbContext();
            };

            var orderRepository = new SqlEventSourcedRepository<Order>(createEventStoreDbContext: eventStoreContext);

            Configuration.Current.UseDependency<IEventSourcedRepository<Order>>(_ => orderRepository);
        }

        private void StopTriggeringConcurrencyExceptions() =>
            Configuration.Current.UseDependency<IEventSourcedRepository<Order>>(_ => new SqlEventSourcedRepository<Order>());

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

            await Schedule(
                    new CreateCommandTarget(Any.CamelCaseName()),
                    Clock.Now().AddDays(1),
                    clock: clockOne);
            await Schedule(
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

        [Test]
        public override async Task When_a_scheduler_clock_is_advanced_then_the_domain_clock_is_coordinated_to_the_scheduler_clock_for_events_written_as_a_result()
        {
            // arrange
            var targetId = Any.CamelCaseName();

            var scheduledCreationTime = Clock.Now().AddDays(1);

            await Schedule(new CreateCommandTarget(targetId), scheduledCreationTime);

            // act
            await AdvanceClock(by: TimeSpan.FromDays(7));

            //assert 
            var target = await Get<NonEventSourcedCommandTarget>(targetId);
            target.Should().NotBeNull();
            target.CreatedTime.Should().Be(scheduledCreationTime);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_and_the_clock_is_advanced_again_then_it_can_be_retried()
        {
            // arrange
            var target = new NonEventSourcedCommandTarget { IsValid = false };

            await Save(target);

            await Schedule(
                target.Id,
                new TestCommand(),
                Clock.Now().AddDays(10));

            // act
            await AdvanceClock(TimeSpan.FromDays(10.1));

            target.CommandsFailed
                .Should()
                .HaveCount(1);
            target
                .CommandsEnacted
                .Should()
                .HaveCount(0);

            target.IsValid = true;

            await AdvanceClock(TimeSpan.FromHours(1));

            target
                .CommandsFailed
                .Should()
                .HaveCount(1);
            target
                .CommandsEnacted
                .Should()
                .HaveCount(1);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_due_to_a_concurrency_exception_then_it_is_retried_by_default()
        {
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            await Save(order);

            TriggerConcurrencyExceptionOnOrderCommands(order.Id);

            await Schedule(order.Id, new Cancel());

            for (var i = 1; i < 6; i++)
            {
                Console.WriteLine("Advancing clock");

                GetScheduledCommandNumberOfAttempts(order.Id)
                    .Should()
                    .Be(i);

                await AdvanceClock(by: TimeSpan.FromDays(20));
            }
        }

        [Test]
        public async Task When_a_scheduled_command_fails_due_to_a_concurrency_exception_then_commands_that_its_handler_scheduled_are_not_duplicated()
        {
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            await Save(new CustomerAccount(order.CustomerId).Apply(new ChangeEmailAddress(Any.Email())));
            await Save(order);

            TriggerConcurrencyExceptionOnOrderCommands(order.Id);

            await Schedule(order.Id, new Cancel());

            for (var i = 1; i < 3; i++)
            {
                await AdvanceClock(by: TimeSpan.FromDays(1));
            }

            StopTriggeringConcurrencyExceptions();

            await AdvanceClock(by: TimeSpan.FromDays(1));

            await SchedulerWorkComplete();

            var customer = await Get<CustomerAccount>(order.CustomerId);

            customer.Events()
                .OfType<CustomerAccount.OrderCancelationConfirmationEmailSent>()
                .Count()
                .Should()
                .Be(1);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_due_to_a_concurrency_exception_then_it_is_not_marked_as_applied()
        {
            // arrange 
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            var ship = new Ship();

            order.Apply(ship);

            order.Apply(new ChargeCreditCardOn
            {
                Amount = 10,
                ChargeDate = Clock.Now().AddDays(10)
            });

            await Save(order);

            TriggerConcurrencyExceptionOnOrderCommands(order.Id);

            // act
            await AdvanceClock(by: TimeSpan.FromDays(20));

            // assert
            using (var db = CommandSchedulerDbContext())
            {
                // make sure we actually triggered a concurrency exception
                db.Errors
                    .Where(e => e.ScheduledCommand.AggregateId == order.Id)
                    .ToArray()
                    .Should()
                    .Contain(e => e.Error.Contains("ConcurrencyException"));

                var scheduledCommand = db.ScheduledCommands.Single(c => c.AggregateId == order.Id);

                scheduledCommand.AppliedTime.Should().NotHaveValue();
                scheduledCommand.Attempts.Should().Be(1);
            }
        }

        [Test]
        public async Task When_two_different_callers_advance_the_same_clock_at_the_same_time_then_commands_are_only_run_once()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            var barrier = new Barrier(2);

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(5)));
            await Save(order);
            var eventCount = order.Events().Count();

            var orderScheduler = Configuration.Current
                .CommandDeliverer<Order>()
                .InterceptDeliver(async (c, next) =>
                {
                    barrier.SignalAndWait(TimeSpan.FromSeconds(10));
                    await next(c);
                });
            Configuration.Current.UseDependency(_ => orderScheduler);

            var caller1 = Task.Run(() => AdvanceClock(TimeSpan.FromDays(10)));
            var caller2 = Task.Run(() => AdvanceClock(TimeSpan.FromDays(10)));

            Task.WaitAll(caller1, caller2);

            (await Get<Order>(order.Id))
                .Events()
                .Count()
                .Should()
                .Be(eventCount + 1, "the scheduled command should only be applied once");
        }

        [Test]
        public async Task When_a_clock_is_advanced_then_resulting_SuccessfulCommands_are_included_in_the_result()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var customerAccountId = Any.Guid();
            await Save(new CustomerAccount(customerAccountId)
                .Apply(new ChangeEmailAddress(Any.Email())));
            var order = CommandSchedulingTests_EventSourced.CreateOrder(customerAccountId: customerAccountId);

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            await Save(order);

            // act
            var result = await AdvanceClock(to: Clock.Now().AddMonths(2));

            //assert 
            result.SuccessfulCommands
                  .Should()
                  .ContainSingle(_ => _.ScheduledCommand
                                       .IfTypeIs<IScheduledCommand<Order>>()
                                       .Then(c => c.TargetId == order.Id.ToString())
                                       .ElseDefault());
        }

        [Test]
        public async Task When_a_command_schedules_another_command_on_a_specific_clock_the_new_command_is_on_the_same_clock()
        {
            // arrange
            var targetId = Any.Guid();
            var nextCommandId = Any.CamelCaseName();
            var theFirst = DateTimeOffset.Parse("2012-01-01");
            var theThird = theFirst.AddDays(2);
            var theFourth = theThird.AddDays(1);

            var customClock = CreateClock(Any.CamelCaseName(), theFirst);

            var delivered = new ConcurrentBag<IScheduledCommand>();
            Configuration.Current
                         .TraceScheduledCommands(
                             onDelivered: command =>
                             {
                                 delivered.Add(command);
                             });
            var target = new CommandSchedulerTestAggregate(targetId);
            await Save(target);

            await Schedule(
                targetId,
                new CommandSchedulerTestAggregate.CommandThatSchedulesAnotherCommand
                {
                    NextCommand = new CommandSchedulerTestAggregate.Command
                    {
                        CommandId = nextCommandId
                    },
                    NextCommandDueTime = theFourth
                },
                theThird,
                clock: customClock);

            // act
            await AdvanceClock(theThird, customClock.Name);
            await AdvanceClock(theFourth, customClock.Name);

            //assert
            delivered.Should().HaveCount(2);
            delivered
                .OfType<ScheduledCommand<CommandSchedulerTestAggregate>>()
                .Select(_ => _.Command)
                .OfType<CommandSchedulerTestAggregate.Command>()
                .Should()
                .Contain(c => c.CommandId == nextCommandId);
        }
    }
}