// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Domain.Tests.Infrastructure;
using Microsoft.Its.Recipes;
using Moq;
using NCrunch.Framework;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerTests_EventSourced : SqlCommandSchedulerTests
    {
        private IEventSourcedRepository<CustomerAccount> accountRepository;
        protected IEventSourcedRepository<Order> orderRepository;
        protected CompositeDisposable disposables;
        protected string clockName;
        protected ISchedulerClockTrigger clockTrigger;
        protected ISchedulerClockRepository clockRepository;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Logging.Configure();

            // disable authorization checks
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [SetUp]
        public void SetUp()
        {
            clockName = Any.CamelCaseName();

            Clock.Reset();

            disposables = new CompositeDisposable
            {
                Disposable.Create(Clock.Reset)
            };

            var configuration = new Configuration()
                .UseSqlEventStore(c => c.UseConnectionString(TestDatabases.EventStore.ConnectionString))
                .UseSqlStorageForScheduledCommands(c => c.UseConnectionString(TestDatabases.CommandScheduler.ConnectionString));

            Configure(configuration);

            disposables.Add(ConfigurationContext.Establish(configuration));
            disposables.Add(configuration);

            orderRepository = configuration.Repository<Order>();
            accountRepository = configuration.Repository<CustomerAccount>();
            clockTrigger = configuration.SchedulerClockTrigger();
            clockRepository = configuration.SchedulerClockRepository();
            clockRepository.CreateClock(clockName, Clock.Now());
        }

        [TearDown]
        public void TearDown()
        {
            Inventory.IsAvailable = sku => true;
            disposables.Dispose();
        }

        [Test]
        public override async Task When_a_clock_is_advanced_its_associated_commands_are_triggered()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            await clockTrigger.AdvanceClock(clockName: clockName,
                                            @by: TimeSpan.FromDays(32));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
            order.ShipmentId
                 .Should()
                 .Be(shipmentId, "Properties should be transferred correctly from the serialized command");
        }

        [Test]
        public async Task When_a_scheduler_clock_is_advanced_then_the_domain_clock_is_coordinated_to_the_scheduler_clock_for_events_written_as_a_result()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            var shipDate = Clock.Now().AddMonths(1).Date;
            order.Apply(new ShipOn(shipDate)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            await clockTrigger.AdvanceClock(clockName: clockName,
                                            @by: TimeSpan.FromDays(33));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            order.Events().OfType<Order.Shipped>()
                 .Last()
                 .Timestamp
                 .Should()
                 .Be(shipDate);
        }

        [Test]
        public async Task When_a_scheduler_clock_is_advanced_then_the_domain_clock_is_unaffected_for_events_written_by_other_aggregates()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            var shipDate = Clock.Now().AddMonths(1).Date;
            order.Apply(new ShipOn(shipDate)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            await clockTrigger.AdvanceClock(clockName: clockName,
                                            @by: TimeSpan.FromDays(32));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            order.Events()
                 .OfType<Order.Shipped>()
                 .Last()
                 .Timestamp
                 .Should()
                 .Be(shipDate);
        }

        [Test]
        public override async Task When_a_clock_is_advanced_then_commands_are_not_triggered_that_have_not_become_due()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(2).Date)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            await clockTrigger.AdvanceClock(clockName: clockName,
                                            @by: TimeSpan.FromDays(30));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<CommandScheduled<Order>>();
        }

        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_domain_clock()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            // act
            order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
            await orderRepository.Save(order);

            await SchedulerWorkComplete();

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }
      
        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_scheduler_clock()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            await clockTrigger.AdvanceClock(clockName, clockRepository.ReadClock(clockName).AddDays(10));

            // act
            var shipOn = clockRepository.ReadClock(clockName).AddDays(-5);

            order.Apply(new ShipOn(shipOn));
            await orderRepository.Save(order);

            await SchedulerWorkComplete();

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public override async Task Immediately_scheduled_commands_triggered_by_a_scheduled_command_have_their_due_time_set_to_the_causative_command_clock()
        {
            VirtualClock.Start();

            var aggregate = new CommandSchedulerTestAggregate();
            var repository = Configuration.Current
                                          .Repository<CommandSchedulerTestAggregate>();

            await repository.Save(aggregate);

            var scheduler = Configuration.Current.CommandScheduler<CommandSchedulerTestAggregate>();

            var dueTime = Clock.Now().AddMinutes(5);

            await scheduler.Schedule(
                aggregate.Id,
                dueTime: dueTime,
                command: new CommandSchedulerTestAggregate.CommandThatSchedulesAnotherCommand
                {
                    NextCommandAggregateId = aggregate.Id,
                    NextCommand = new CommandSchedulerTestAggregate.Command
                    {
                        CommandId = Any.CamelCaseName()
                    }
                });

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                foreach (var command in db.ScheduledCommands.Where(c => c.AggregateId == aggregate.Id))
                {
                    command.AppliedTime
                           .IfNotNull()
                           .ThenDo(v => v.Should().BeCloseTo(dueTime, 10));
                }
            }
        }

        [Test]
        public override async Task Scheduled_commands_with_no_due_time_set_the_correct_clock_time_when_delivery_is_deferred()
        {
            // arrange
            var deliveredTime = new DateTimeOffset();
            var configuration = Configuration.Current;
            var clockTrigger = configuration.SchedulerClockTrigger();
            await clockTrigger.AdvanceClock(clockName, DateTimeOffset.Parse("2046-02-13 01:00:00 AM"));
            configuration
                .UseCommandHandler<Order, CreateOrder>(async (_, __) => deliveredTime = Clock.Now());

            // act
            await configuration.CommandScheduler<Order>()
                               .Schedule(Any.Guid(),
                                         new CreateOrder(Any.FullName())
                                         {
                                             CanBeDeliveredDuringScheduling = false
                                         },
                                         dueTime: null);

            await clockTrigger
                .AdvanceClock(clockName, by: 1.Hours());

            // assert 
            deliveredTime.Should().Be(DateTimeOffset.Parse("2046-02-13 01:00:00 AM"));
        }

        [Test]
        public async Task When_a_clock_is_advanced_then_unassociated_commands_are_not_triggered()
        {
            // arrange
            var clockOne = Any.CamelCaseName();
            var clockTwo = Any.CamelCaseName();

            clockRepository.CreateClock(clockTwo, Clock.Now());

            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(2).Date)
            {
                ShipmentId = clockOne
            });
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = clockOne;
            await orderRepository.Save(order);

            // act
            await clockTrigger.AdvanceClock(clockName: clockTwo, @by: TimeSpan.FromDays(30));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<CommandScheduled<Order>>();
        }

        [Test]
        public void A_clock_cannot_be_moved_to_a_prior_time()
        {
            // arrange
            var name = Any.AlphanumericString(8, 8);
            clockRepository.CreateClock(name, DateTimeOffset.UtcNow);

            // act
            Action moveBackwards = () => clockTrigger.AdvanceClock(name, DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(5))).Wait();

            // assert
            moveBackwards.ShouldThrow<InvalidOperationException>()
                         .And
                         .Message
                         .Should()
                         .Contain("A clock cannot be moved backward");
        }

        [Test]
        public void Two_clocks_cannot_be_created_having_the_same_name()
        {
            var name = Any.CamelCaseName();
            clockRepository.CreateClock(name, DateTimeOffset.UtcNow);

            Action createAgain = () =>
                                 clockRepository.CreateClock(name, DateTimeOffset.UtcNow.AddDays(1));

            createAgain.ShouldThrow<ConcurrencyException>()
                       .And
                       .Message
                       .Should()
                       .Contain($"A clock named '{name}' already exists");
        }

        [Test]
        public async Task When_a_scheduled_command_fails_and_the_clock_is_advanced_again_then_it_can_be_retried()
        {
            // ARRANGE
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress
                {
                    NewEmailAddress = Any.Email()
                });

            account
                .Apply(new SendMarketingEmailOn(Clock.Now().AddDays(5)))
                .Apply(new RequestNoSpam());
            await accountRepository.Save(account);

            // ACT
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(6));
            account.CommunicationsSent.Count().Should().Be(0);

            // requesting spam will unblock the original scheduled command if it is re-attempted
            account = await accountRepository.GetLatest(account.Id);
            account.Apply(new RequestSpam());
            await accountRepository.Save(account);
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromMinutes(1));

            // ASSERT 
            account = await accountRepository.GetLatest(account.Id);
            account.CommunicationsSent.Count().Should().Be(1);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_due_to_a_concurrency_exception_then_it_is_retried_by_default()
        {
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            await orderRepository.Save(order);

            TriggerConcurrencyExceptionOnOrderCommands(order.Id);

            var orderScheduler = Configuration.Current.CommandScheduler<Order>();
            await orderScheduler
                .Schedule(new CommandScheduled<Order>
                {
                    AggregateId = order.Id,
                    Command = new Cancel()
                });

            for (var i = 1; i < 6; i++)
            {
                Console.WriteLine("Advancing clock");
                GetScheduledCommandNumberOfAttempts(order.Id).Should().Be(i);
                await clockTrigger.AdvanceClock(clockName, @by: TimeSpan.FromDays(20));
            }
        }

        [Test]
        public async Task When_a_scheduled_command_fails_due_to_a_concurrency_exception_then_commands_that_its_handler_scheduled_are_not_duplicated()
        {
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            await accountRepository.Save(new CustomerAccount(order.CustomerId).Apply(new ChangeEmailAddress(Any.Email())));
            await orderRepository.Save(order);

            TriggerConcurrencyExceptionOnOrderCommands(order.Id);

            var orderScheduler = Configuration.Current.CommandScheduler<Order>();
            await orderScheduler
                .Schedule(new CommandScheduled<Order>
                {
                    AggregateId = order.Id,
                    Command = new Cancel()
                });

            for (var i = 1; i < 3; i++)
            {
                await clockTrigger.AdvanceClock(clockName, @by: TimeSpan.FromDays(1));
            }

            StopTriggeringConcurrencyExceptions();

            await clockTrigger.AdvanceClock(clockName, @by: TimeSpan.FromDays(1));

            await SchedulerWorkComplete();

            var customer = await accountRepository.GetLatest(order.CustomerId);

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

            await orderRepository.Save(order);

            TriggerConcurrencyExceptionOnOrderCommands(order.Id);

            // act
            await Configuration
                .Current
                .SchedulerClockTrigger()
                .AdvanceClock(clockName, @by: TimeSpan.FromDays(20));

            // assert
            using (var db = Configuration.Current.CommandSchedulerDbContext())
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
        public async Task When_Save_fails_then_a_scheduled_command_error_is_recorded()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            var innerRepository = new SqlEventSourcedRepository<Order>();
            var saveCount = 0;
            Configuration.Current
                         .Container
                         .Register<IEventSourcedRepository<Order>>(c => new FakeEventSourcedRepository<Order>(innerRepository)
                         {
                             OnSave = async o =>
                             {
                                 saveCount++;

                                 // throw on the second save attempt, which is when the clock is advanced delivering the scheduled command
                                 if (saveCount == 2)
                                 {
                                     throw new Exception("oops!");
                                 }
                                 await innerRepository.Save(o);
                             }
                         });

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(30)));
            await Configuration.Current.Repository<Order>().Save(order);
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(31));

            //assert 
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var error = db.Errors.Single(c => c.ScheduledCommand.AggregateId == order.Id).Error;
                error.Should().Contain("oops!");
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
            await orderRepository.Save(order);
            var eventCount = order.Events().Count();

            var orderScheduler = Configuration.Current
                                              .CommandScheduler<Order>()
                                              .Wrap(deliver: async (c, next) =>
                                              {
                                                  barrier.SignalAndWait(TimeSpan.FromSeconds(10));
                                                  await next(c);
                                              });
            Configuration.Current.UseDependency(_ => orderScheduler);

            var caller1 = Task.Run(() => clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(10)));
            var caller2 = Task.Run(() => clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(10)));

            try
            {
                Task.WaitAll(caller1, caller2);
            }
            catch (DataException ex)
            {
                Console.WriteLine("Expected exception thrown:\n" + ex);
            }

            (await orderRepository.GetLatest(order.Id))
                .Events()
                .Count()
                .Should()
                .Be(eventCount + 1, "the scheduled command should only be applied once");
        }
        
        [Test]
        public async Task When_a_scheduled_command_fails_then_the_events_written_by_the_command_handler_are_not_saved_to_the_aggregate()
        {
            var aggregate = new CommandSchedulerTestAggregate();
            var repository = Configuration.Current.Repository<CommandSchedulerTestAggregate>();

            await repository.Save(aggregate);

            var scheduler = Configuration.Current.CommandScheduler<CommandSchedulerTestAggregate>();

            await
                scheduler.Schedule(
                    aggregate.Id,
                    command: new CommandSchedulerTestAggregate.CommandThatRecordsCommandSucceededEventWithoutExplicitlySavingAndThenFails());

            var latestAggregate = await repository.GetLatest(aggregate.Id);

            latestAggregate.EventHistory.OfType<CommandSchedulerTestAggregate.CommandSucceeded>().Should().HaveCount(0);
        }

        [Test]
        public async Task A_command_handler_can_control_retries_of_a_failed_command()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(30)
                });
            await orderRepository.Save(order);

            // act
            // initial attempt fails
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(31));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // two more attempts
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(1));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(1));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // final attempt results in giving up
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(1));
            order = await orderRepository.GetLatest(order.Id);
            var last = order.Events().Last();
            last.Should().BeOfType<Order.Cancelled>();
            last.As<Order.Cancelled>().Reason.Should().Be("Final credit card charge attempt failed.");
        }

        [Test]
        public override async Task A_command_handler_can_request_retry_of_a_failed_command_as_soon_as_possible()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(30),
                    ChargeRetryPeriod = TimeSpan.Zero
                });
            await orderRepository.Save(order);

            // act
            // initial attempt fails
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(30.1));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // two more attempts, advancing the clock only a tick at a time
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromTicks(1));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromTicks(1));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // final attempt results in giving up
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromTicks(1));
            order = await orderRepository.GetLatest(order.Id);
            var last = order.Events().Last();
            last.Should().BeOfType<Order.Cancelled>();
            last.As<Order.Cancelled>().Reason.Should().Be("Final credit card charge attempt failed.");
        }

        [Test]
        public override async Task A_command_handler_can_request_retry_of_a_failed_command_as_late_as_it_wants()
        {
            var order = CommandSchedulingTests_EventSourced.CreateOrder();
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(1),
                    ChargeRetryPeriod = TimeSpan.FromDays(7)
                });
            await orderRepository.Save(order);

            // act
            // initial attempt fails
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(4));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // ship the order so the next retry will succeed
            order.Apply(new Ship());
            await orderRepository.Save(order);

            // advance the clock a couple of days, which doesn't trigger a retry yet
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(2));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Should().NotContain(e => e is Order.CreditCardCharged);

            // advance the clock enough to trigger a retry
            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(5));
            order = await orderRepository.GetLatest(order.Id);
            order.Events().Should().Contain(e => e is Order.CreditCardCharged);
        }

        [Test]
        public override async Task A_command_handler_can_cancel_a_scheduled_command_after_it_fails()
        {
            var order = new Order(
                new CreateOrder(Any.FullName()))
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
                    Country = "USA"
                })
                .Apply(new ShipOn(Clock.Now().AddDays(10)))
                .Apply(new Ship());

            await orderRepository.Save(order);

            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query = cmds =>
                                                                                     cmds.Where(cmd => cmd.AggregateId == order.Id)
                                                                                         .Where(cmd => cmd.AppliedTime == null &&
                                                                                                       cmd.FinalAttemptTime == null);

            var result = await clockTrigger.Trigger(query);

            // the command should fail validation 
            result.FailedCommands.Count().Should().Be(1);

            result = await clockTrigger.Trigger(query);

            result.FailedCommands.Count().Should().Be(0);
        }

        [Test]
        public async Task A_scheduled_command_can_schedule_other_commands()
        {
            // ARRANGE
            var email = Any.Email();
            Console.WriteLine(new { clockName, email });
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress(email))
                .Apply(new SendMarketingEmailOn(Clock.Now().AddDays(1)))
                .Apply(new RequestSpam());
            await accountRepository.Save(account);

            // ACT
            var result = await clockTrigger.AdvanceClock(clockName, TimeSpan.FromDays(10));
            Console.WriteLine(result.ToLogString());
           
            await SchedulerWorkComplete();

            // ASSERT
            account = await accountRepository.GetLatest(account.Id);

            Console.WriteLine(account.Events()
                                     .Select(e => string.Format("{0}: {1}{2}\n",
                                                                e.EventName(),
                                                                e.Timestamp,
                                                                e.IfTypeIs<IScheduledCommand<CustomerAccount>>()
                                                                 .Then(s => " -->  DUE: " + s.DueTime)
                                                                 .ElseDefault()))
                                     .ToLogString());

            account.Events()
                   .OfType<CommandScheduled<CustomerAccount>>()
                   .Count()
                   .Should()
                   .Be(3);
            account.Events()
                   .Last()
                   .Should()
                   .BeOfType<CommandScheduled<CustomerAccount>>();
        }

        [Test]
        public override async Task Specific_scheduled_commands_can_be_triggered_directly_by_target_id()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            await clockTrigger.Trigger(commands => commands.Where(cmd => cmd.AggregateId == order.Id));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
            order.ShipmentId
                 .Should()
                 .Be(shipmentId);

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == order.Id)
                  .Should()
                  .OnlyContain(c => c.AppliedTime != null);
            }
        }

        [Test]
        public async Task When_a_command_is_triggered_and_succeeds_then_result_SuccessfulCommands_references_it()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var customerAccountId = Any.Guid();
            await accountRepository.Save(new CustomerAccount(customerAccountId)
                                             .Apply(new ChangeEmailAddress(Any.Email())));
            var order = CommandSchedulingTests_EventSourced.CreateOrder(customerAccountId: customerAccountId);

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            var result = await clockTrigger.Trigger(commands => commands.Where(cmd => cmd.AggregateId == order.Id));

            //assert 
            result.SuccessfulCommands
                  .Should()
                  .ContainSingle(a => a.ScheduledCommand
                                       .IfTypeIs<IScheduledCommand<Order>>()
                                       .Then(c => c.TargetId == order.Id.ToString())
                                       .ElseDefault());
        }

        [Test]
        public async Task When_a_clock_is_advanced_then_resulting_SuccessfulCommands_are_included_in_the_result()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var customerAccountId = Any.Guid();
            await accountRepository.Save(new CustomerAccount(customerAccountId)
                                             .Apply(new ChangeEmailAddress(Any.Email())));
            var order = CommandSchedulingTests_EventSourced.CreateOrder(customerAccountId: customerAccountId);

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            await orderRepository.Save(order);

            // act
            var result = await clockTrigger.AdvanceClock(clockName, Clock.Now().AddMonths(2));

            //assert 
            result.SuccessfulCommands
                  .Should()
                  .ContainSingle(a => a.ScheduledCommand
                                       .IfTypeIs<IScheduledCommand<Order>>()
                                       .Then(c => c.TargetId == order.Id.ToString())
                                       .ElseDefault());
        }

        [Test]
        public override async Task When_triggering_specific_commands_then_the_result_can_be_used_to_evaluate_failures()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var customerAccountId = Any.Guid();
            await accountRepository.Save(new CustomerAccount(customerAccountId)
                                             .Apply(new ChangeEmailAddress(Any.Email())));
            var order = CommandSchedulingTests_EventSourced.CreateOrder(customerAccountId: customerAccountId);

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            })
                 .Apply(new Cancel());
            await orderRepository.Save(order);

            // act
            var result = await clockTrigger.Trigger(commands => commands.Where(cmd => cmd.AggregateId == order.Id));

            //assert 
            order = await orderRepository.GetLatest(order.Id);

            result.FailedCommands.Count().Should().Be(1);
            result.FailedCommands.Single()
                  .Exception
                  .ToString()
                  .Should()
                  .Contain("The order has been cancelled");
        }

        [Test]
        public async Task When_a_command_is_delivered_a_second_time_with_the_same_ETag_it_is_not_retried_afterward()
        {
            // arrange
            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = Any.Guid()
            });
            await orderRepository.Save(order);

            var command = new AddItem
            {
                ProductName = Any.Word(),
                Price = 10m,
                ETag = Any.Guid().ToString()
            };

            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            // act
            await commandScheduler.Schedule(order.Id, command, Clock.Now().AddDays(1));
            Thread.Sleep(1); // the sequence number is set from the current tick count, which every now and then produces a duplicate here 
            await commandScheduler.Schedule(order.Id, command, Clock.Now().AddDays(1));
            await clockTrigger.Trigger(cmd => cmd.Where(c => c.AggregateId == order.Id));

            // assert
            order = await orderRepository.GetLatest(order.Id);

            order.Balance.Should().Be(10);

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == order.Id)
                  .Should()
                  .OnlyContain(c => c.AppliedTime != null);

                db.Errors
                  .Where(c => c.ScheduledCommand.AggregateId == order.Id)
                  .Should()
                  .BeEmpty();
            }
        }

        [Test]
        public override async Task When_a_command_is_scheduled_but_an_exception_is_thrown_in_a_handler_then_an_error_is_recorded()
        {
            Configuration.Current.UseDependency(_ => new CustomerAccount.OrderEmailConfirmer
            {
                SendOrderConfirmationEmail = x => { throw new Exception("drat!"); }
            });

            // create a customer account
            var customer = new CustomerAccount(Any.Guid())
                .Apply(new ChangeEmailAddress(Any.Email()));
            await accountRepository.Save(customer);

            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customer.Id
            });
            await orderRepository.Save(order);

            // act
            order.Apply(new Cancel());
            await orderRepository.Save(order);

            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromMinutes(1.2));

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.Errors
                  .Where(e => e.ScheduledCommand.AggregateId == customer.Id)
                  .Should()
                  .Contain(e => e.Error.Contains("drat!"));
            }
        }

        [Test]
        public async Task When_a_command_is_scheduled_but_the_event_that_triggered_it_was_not_successfully_written_then_the_command_is_not_applied()
        {
            VirtualClock.Start();

            // create a customer account
            var customer = new CustomerAccount(Any.Guid())
                .Apply(new ChangeEmailAddress(Any.Email()));
            await accountRepository.Save(customer);

            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customer.Id
            });

            // act
            // cancel the order but don't save it
            order.Apply(new Cancel());

            // assert
            // verify that the customer did not receive the scheduled NotifyOrderCanceled command
            customer = await accountRepository.GetLatest(customer.Id);
            customer.Events().Last().Should().BeOfType<CustomerAccount.EmailAddressChanged>();

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == customer.Id)
                  .Should()
                  .ContainSingle(c => c.AppliedTime == null);
            }
        }

        [Test]
        public override async Task When_a_command_is_scheduled_but_the_target_it_applies_to_is_not_found_then_the_command_is_retried()
        {
            // create and cancel an order for a nonexistent customer account 
            var customerId = Any.Guid();
            Console.WriteLine(new { customerId });
            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customerId
            });

            order.Apply(new Cancel());
            await orderRepository.Save(order);

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == customerId)
                  .Should()
                  .ContainSingle(c => c.AppliedTime == null);
            }

            // act
            // now save the customer and advance the clock
            await accountRepository.Save(new CustomerAccount(customerId).Apply(new ChangeEmailAddress(Any.Email())));

            await clockTrigger.AdvanceClock(clockName, TimeSpan.FromMinutes(2));

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == customerId)
                  .Should()
                  .ContainSingle(c => c.AppliedTime != null);

                var customer = await accountRepository.GetLatest(customerId);
                customer.Events()
                        .Last()
                        .Should().BeOfType<CustomerAccount.OrderCancelationConfirmationEmailSent>();
            }
        }

        [Test]
        public async Task When_a_scheduler_assigned_negative_SequenceNumber_collides_then_it_retries_scheduling_with_a_new_SequenceNumber()
        {
            // arrange
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var initialSequenceNumber = -Any.PositiveInt();

            var orderId = Any.Guid();

            await commandScheduler.Schedule(new CommandScheduled<Order>
            {
                AggregateId = orderId,
                Command = new Cancel(),
                SequenceNumber = initialSequenceNumber,
                DueTime = Clock.Now().AddYears(1)
            });

            var secondCommand = new CommandScheduled<Order>
            {
                AggregateId = orderId,
                Command = new Cancel(),
                SequenceNumber = initialSequenceNumber,
                DueTime = Clock.Now().AddYears(1)
            };

            await commandScheduler.Schedule(secondCommand);

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Count(c => c.AggregateId == orderId)
                  .Should()
                  .Be(2);
            }
        }

        [Test]
        public async Task When_a_non_scheduler_assigned_negative_SequenceNumber_collides_then_it_is_ignored()
        {
            // arrange
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var db = Configuration.Current.CommandSchedulerDbContext();

            disposables.Add(db);

            var initialSequenceNumber = Any.PositiveInt();

            var orderId = Any.Guid();

            await commandScheduler.Schedule(new CommandScheduled<Order>
            {
                AggregateId = orderId,
                Command = new Cancel(),
                SequenceNumber = initialSequenceNumber,
                DueTime = Clock.Now().AddYears(1)
            });

            var secondCommand = new CommandScheduled<Order>
            {
                AggregateId = orderId,
                Command = new Cancel(),
                SequenceNumber = initialSequenceNumber,
                DueTime = Clock.Now().AddYears(1)
            };

            Action again = () => commandScheduler.Schedule(secondCommand).Wait();

            again.ShouldNotThrow<DbUpdateException>();
        }

        [Test]
        public override async Task Constructor_commands_can_be_scheduled_to_create_new_aggregate_instances()
        {
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            await commandScheduler.Schedule(orderId, new CreateOrder(customerName)
            {
                AggregateId = orderId
            });

            var order = await Configuration.Current.Container.Resolve<IEventSourcedRepository<Order>>().GetLatest(orderId);

            order.Should().NotBeNull();
            order.CustomerName.Should().Be(customerName);
        }

        [Test]
        public override async Task When_a_constructor_command_fails_with_a_ConcurrencyException_it_is_not_retried()
        {
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            await commandScheduler.Schedule(orderId, new CreateOrder(customerName)
            {
                AggregateId = orderId
            });

            await commandScheduler.Schedule(orderId, new CreateOrder(customerName)
            {
                AggregateId = orderId
            });

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var commands = db.ScheduledCommands
                                 .Where(c => c.AggregateId == orderId)
                                 .OrderBy(c => c.CreatedTime)
                                 .ToArray();

                Console.WriteLine(commands.ToDiagnosticJson());
                commands.Length.Should().Be(2);
                commands
                    .Should()
                    .ContainSingle(c => c.FinalAttemptTime == null);
            }
        }

        [Test]
        public override async Task When_an_immediately_scheduled_command_depends_on_a_precondition_that_has_not_been_met_yet_then_there_is_not_initially_an_attempt_recorded()
        {
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = Any.Guid(),
                ETag = Any.Guid().ToString()
            };
           
            var scheduledCommand =   await commandScheduler.Schedule(orderId,
                                                                     new CreateOrder(customerName)
                                                                     {
                                                                         AggregateId = orderId
                                                                     },
                                                                     deliveryDependsOn: prerequisiteEvent);

            var order = await orderRepository.GetLatest(orderId);

            order.Should().BeNull();

            scheduledCommand.Result.Should().BeOfType<CommandScheduled>();

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var command = db.ScheduledCommands.Single(c => c.AggregateId == orderId);

                command.AppliedTime
                       .Should()
                       .NotHaveValue();

                command.Attempts
                       .Should()
                       .Be(0);
            }
        }

        [Test]
        public async Task When_an_immediately_scheduled_command_depends_on_an_event_then_delivery_in_memory_waits_on_the_event_being_saved()
        {
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            var prerequisiteAggregateId = Any.Guid();
            var prerequisiteETag = Any.Guid().ToString();

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = prerequisiteAggregateId,
                ETag = prerequisiteETag
            };

            await commandScheduler.Schedule(orderId,
                                            new CreateOrder(customerName)
                                            {
                                                AggregateId = orderId
                                            },
                                            deliveryDependsOn: prerequisiteEvent);

            // sanity check that the order is null
            var order = await orderRepository.GetLatest(orderId);
            order.Should().BeNull();

            await accountRepository.Save(new CustomerAccount(prerequisiteAggregateId)
                                             .Apply(new ChangeEmailAddress(Any.Email())
                                             {
                                                 ETag = prerequisiteETag
                                             }));

            await SchedulerWorkComplete();

            // assert
            // now the order should have been created
            order = await orderRepository.GetLatest(orderId);
            order.Should().NotBeNull();
        }

        [Test]
        public override async Task When_a_scheduled_command_depends_on_an_event_that_never_arrives_it_is_eventually_abandoned()
        {
            VirtualClock.Start();
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();

            var orderId = Any.Guid();
            await orderRepository.Save(new Order(new CreateOrder(Any.FullName())
            {
                AggregateId = orderId
            }));

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = Any.Guid(),
                ETag = Any.Guid().ToString()
            };

            await commandScheduler.Schedule(orderId,
                                            new AddItem
                                            {
                                                ProductName = Any.Paragraph(3),
                                                Price = 10m
                                            },
                                            deliveryDependsOn: prerequisiteEvent);

            for (var i = 0; i < 7; i++)
            {
                VirtualClock.Current.AdvanceBy(TimeSpan.FromMinutes(90));
                Console.WriteLine(
                    (await clockTrigger.Trigger(commands => commands.Due().Where(c => c.AggregateId == orderId))).ToLogString());
            }

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var command = db.ScheduledCommands.Single(c => c.AggregateId == orderId);

                command.AppliedTime
                       .Should()
                       .NotHaveValue();

                command.Attempts
                       .Should()
                       .BeGreaterOrEqualTo(5);

                command.FinalAttemptTime
                       .Should()
                       .HaveValue();
            }
        }

        [Test]
        public override async Task When_command_is_durable_but_immediate_delivery_succeeds_then_it_is_not_redelivered()
        {
            var scheduleCount = 0;
            var deliverCount = 0;

            VirtualClock.Start();

            var orderId = Any.Guid();

            var orderScheduler = Configuration.Current
                                              .CommandScheduler<Order>()
                                              .Wrap(
                                                  schedule: async (c, next) =>
                                                  {
                                                      scheduleCount++;
                                                      await next(c);
                                                  },
                                                  deliver: async (c, next) =>
                                                  {
                                                      deliverCount++;
                                                      await next(c);
                                                  });

            Configuration.Current.UseDependency(_ => orderScheduler);

            await Configuration.Current
                               .CommandScheduler<Order>()
                               .Schedule(
                                   new CommandScheduled<Order>
                                   {
                                       DueTime = Clock.Now().AddDays(-1),
                                       AggregateId = orderId,
                                       Command = new CreateOrder(Any.FullName())
                                       {
                                           AggregateId = orderId
                                       }
                                   });

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            scheduleCount.Should().Be(1);
            deliverCount.Should().Be(1);
        }

        [Test]
        public async Task When_a_command_is_non_durable_then_immediate_scheduling_does_not_result_in_a_command_scheduler_db_entry()
        {
            var commandScheduler = Configuration.Current.CommandScheduler<CustomerAccount>();
            var reserverationService = new Mock<IReservationService>();
            reserverationService.Setup(m => m.Reserve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                                .Returns(() => Task.FromResult(true));
            Configuration.Current.UseDependency(_ => reserverationService.Object);

            var aggregateId = Any.Guid();
            await accountRepository.Save(new CustomerAccount(aggregateId).Apply(new RequestNoSpam()));
            await commandScheduler.Schedule(aggregateId, new RequestUserName
            {
                UserName = Any.Email()
            });

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var scheduledAggregateIds = db.ScheduledCommands
                                              .Where(c => aggregateId == c.AggregateId)
                                              .ToArray();

                scheduledAggregateIds.Should()
                                     .BeEmpty();
            }
        }

        [Test]
        public override async Task When_a_clock_is_advanced_and_a_command_fails_to_be_deserialized_then_other_commands_are_still_applied()
        {
            var commandScheduler = Configuration.Current.CommandScheduler<Order>();
        
            var failedAggregateId = Any.Guid();
            var successfulAggregateId = Any.Guid();

            await commandScheduler.Schedule(failedAggregateId,
                                            new CreateOrder(Any.FullName()),
                                            Clock.Now().AddHours(1)); 
            await commandScheduler.Schedule(successfulAggregateId,
                                            new CreateOrder(Any.FullName())
                                            {
                                                AggregateId = successfulAggregateId
                                            },
                                            Clock.Now().AddHours(1.5));

            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var command = db.ScheduledCommands.Single(c => c.AggregateId == failedAggregateId);
                var commandBody = command.SerializedCommand.FromJsonTo<dynamic>();
                commandBody.Command.CustomerId = "not a guid";
                command.SerializedCommand = commandBody.ToString();
                db.SaveChanges();
            }

            // act
            Action advanceClock = () => clockTrigger.AdvanceClock(clockName: clockName,
                                                                  @by: TimeSpan.FromHours(2)).Wait();

            // assert
            advanceClock.ShouldNotThrow();

            var repository = Configuration.Current.Repository<Order>();
            var successfulAggregate = await repository.GetLatest(successfulAggregateId);
            successfulAggregate.Should().NotBeNull();
        }

        [Test]
        public override async Task When_a_clock_is_set_on_a_command_then_it_takes_precedence_over_GetClockName()
        {
            // arrange
            var clockName = Any.CamelCaseName();
            var create = new CreateOrder(Any.FullName())
            {
                AggregateId = Any.Guid()
            };

            var clock = new CommandScheduler.Clock
            {
                Name = clockName,
                UtcNow = DateTimeOffset.Parse("2016-03-01 02:00:00 AM")
            };

            using (var commandScheduler = Configuration.Current.CommandSchedulerDbContext())
            {
                commandScheduler.Clocks.Add(clock);
                commandScheduler.SaveChanges();
            }

            var scheduledCommand = new ScheduledCommand<Order>(
                aggregateId: create.AggregateId,
                command: create,
                dueTime: DateTimeOffset.Parse("2016-03-20 09:00:00 AM"))
            {
                Clock = clock
            };

            // act
            var configuration = Configuration.Current;
            await configuration.CommandScheduler<Order>().Schedule(scheduledCommand);

            await configuration.SchedulerClockTrigger()
                               .AdvanceClock(clockName,
                                             by: 30.Days());

            //assert 
            var target = await configuration.Repository<Order>().GetLatest(create.AggregateId);

            target.Should().NotBeNull();
        }

        protected override void Configure(Configuration configuration) =>
            configuration
                .UseDependency<GetClockName>(c => e => clockName)
                .UseSqlStorageForScheduledCommands(c => c.UseConnectionString(TestDatabases.CommandScheduler.ConnectionString))
                .TraceScheduledCommands();

        protected void TriggerConcurrencyExceptionOnOrderCommands(Guid orderId)
        {
            Configuration.Current.UseDependency(_ => orderRepository);
            ((SqlEventSourcedRepository<Order>) orderRepository).GetEventStoreContext = () =>
            {
                // quick, add a new event in order to trigger a concurrency exception at the moment the scheduler tries to apply the command
                var repository = new SqlEventSourcedRepository<Order>();
                var o = repository.GetLatest(orderId).Result;
                o.Apply(new Annotate<Order>("triggering a concurrency exception", Any.Guid().ToString()));
                repository.Save(o).Wait();

                return EventStoreDbContext();
            };
        }

        protected void StopTriggeringConcurrencyExceptions() => 
            ((SqlEventSourcedRepository<Order>) orderRepository).GetEventStoreContext = () => EventStoreDbContext();

        protected int GetScheduledCommandNumberOfAttempts(Guid aggregateId)
        {
            using (var db = Configuration.Current.CommandSchedulerDbContext())
            {
                var scheduledCommand = db.ScheduledCommands.SingleOrDefault(c => c.AggregateId == aggregateId);
                return scheduledCommand.IfNotNull()
                                       .Then(c => c.Attempts)
                                       .ElseDefault();
            }
        }

        protected async Task SchedulerWorkComplete() =>
            await clockTrigger.Done(clockName);
    }
}