// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
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
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlCommandSchedulerTests : EventStoreDbTest
    {
        private SqlEventSourcedRepository<CustomerAccount> accountRepository;
        private SqlEventSourcedRepository<Order> orderRepository;
        private FakeEventBus bus;
        private CompositeDisposable disposables;
        private SqlCommandScheduler scheduler;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Logging.Configure();
            Formatter<SchedulerAdvancedResult>.RegisterForAllMembers();
            Formatter<ScheduledCommandSuccess>.RegisterForAllMembers();
            Formatter<ScheduledCommandFailure>.RegisterForAllMembers();

            // disable authorization checks
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            disposables = new CompositeDisposable();
            bus = new FakeEventBus();
            orderRepository = new SqlEventSourcedRepository<Order>(bus);
            accountRepository = new SqlEventSourcedRepository<CustomerAccount>(bus);

            var configuration = new Configuration();
            configuration.UseEventBus(bus)
                         .UseSqlEventStore()
                         .UseSqlCommandScheduling()
                         .UseDependency<IEventSourcedRepository<Order>>(t => orderRepository)
                         .UseDependency<IEventSourcedRepository<CustomerAccount>>(t => accountRepository);

            scheduler = configuration.SqlCommandScheduler();

            disposables.Add(scheduler.Activity.Subscribe(s => Console.WriteLine("SqlCommandScheduler: " + s.ToLogString())));
            disposables.Add(ConfigurationContext.Establish(configuration));

            Clock.Reset();
            disposables.Add(Disposable.Create(Clock.Reset));

            using (var db = new CommandSchedulerDbContext())
            {
                db.Clocks
                  .SingleOrDefault(c => c.Name == "default")
                  .IfNotNull()
                  .ThenDo(c => c.UtcNow = Clock.Now());
                db.SaveChanges();
            }
        }

        [TearDown]
        public override void TearDown()
        {
            Inventory.IsAvailable = sku => true;
            disposables.Dispose();
            base.TearDown();
        }

        [Test]
        public void When_a_clock_is_advanced_its_associated_commands_are_triggered()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = shipmentId;
            orderRepository.Save(order);

            // act
            scheduler.AdvanceClock(clockName: shipmentId,
                                   by: TimeSpan.FromDays(32));

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
            order.ShipmentId
                 .Should()
                 .Be(shipmentId, "Properties should be transferred correctly from the serialized command");
        }

        [Test]
        public void When_a_virtual_clock_is_advanced_then_the_domain_clock_is_coordinated_to_the_scheduler_clock_for_events_written_as_a_result()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();

            var shipDate = Clock.Now().AddMonths(1).Date;
            order.Apply(new ShipOn(shipDate)
            {
                ShipmentId = shipmentId
            });
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = shipmentId;
            orderRepository.Save(order);

            // act
            scheduler.AdvanceClock(clockName: shipmentId,
                                   by: TimeSpan.FromDays(33));

            Console.WriteLine(scheduler.ReadClock(shipmentId));

            //assert 
            order = orderRepository.GetLatest(order.Id);
            order.Events().OfType<Order.Shipped>()
                 .Last()
                 .Timestamp
                 .Should()
                 .Be(shipDate);
        }

        [Test]
        public void When_a_virtual_clock_is_advanced_then_the_domain_clock_is_unaffected_for_events_written_by_other_aggregates()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();

            var shipDate = Clock.Now().AddMonths(1).Date;
            order.Apply(new ShipOn(shipDate)
            {
                ShipmentId = shipmentId
            });
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = shipmentId;
            orderRepository.Save(order);

            // act
            scheduler.AdvanceClock(clockName: shipmentId,
                                   by: TimeSpan.FromDays(32));

            //assert 
            order = orderRepository.GetLatest(order.Id);
            order.Events().OfType<Order.Shipped>()
                 .Last()
                 .Timestamp
                 .Should()
                 .Be(shipDate);
        }

        [Test]
        public void When_a_clock_is_advanced_then_commands_are_not_triggered_that_have_not_become_due()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(2).Date)
            {
                ShipmentId = shipmentId
            });
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = shipmentId;
            orderRepository.Save(order);

            // act
            scheduler.AdvanceClock(clockName: shipmentId,
                                   by: TimeSpan.FromDays(30));

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<CommandScheduled<Order>>();
        }

        [Test]
        public void SqlCommandScheduler_executes_scheduled_commands_immediately_if_a_past_due_time_is_specified_on_the_realtime_clock()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            // act
            order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
            orderRepository.Save(order);

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public void SqlCommandScheduler_Activity_is_notified_when_a_command_is_delivered_immediately()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            var activity = new List<ScheduledCommandResult>();

            using (Configuration.Current.Container
                                .Resolve<SqlCommandScheduler>()
                                .Activity
                                .Subscribe(activity.Add))
            {
                // act
                order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
                orderRepository.Save(order);

                //assert 
                activity.Should()
                        .ContainSingle(a => a.ScheduledCommand.AggregateId == order.Id &&
                                            a is ScheduledCommandSuccess);
            }
        }

        [Test]
        public void SqlCommandScheduler_Activity_is_notified_when_a_command_is_delivered_via_Trigger()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();

            var activity = new List<ScheduledCommandResult>();

            order.Apply(new ShipOn(Clock.Now().Add(TimeSpan.FromDays(2))));
            orderRepository.Save(order);

            using (scheduler.Activity.Subscribe(activity.Add))
            {
                // act
                scheduler.AdvanceClock("default", TimeSpan.FromDays(3));

                //assert 
                activity.Should()
                        .ContainSingle(a => a.ScheduledCommand.AggregateId == order.Id &&
                                            a is ScheduledCommandSuccess);
            }
        }

        [Test]
        public void SqlCommandScheduler_executes_scheduled_commands_immediately_if_a_past_due_time_is_specified_on_a_virtual_clock()
        {
            // arrange
            var order = CommandSchedulingTests.CreateOrder();
            var clockName = Any.AlphanumericString(9, 9);
            scheduler.CreateClock(clockName, Clock.Now().AddDays(10));

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(5)));
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = clockName;
            orderRepository.Save(order);

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public void When_a_clock_is_advanced_then_unassociated_commands_are_not_triggered()
        {
            // arrange
            var clockOne = Any.AlphanumericString(8, 8);
            var clockTwo = Any.AlphanumericString(8, 8);

            scheduler.CreateClock(clockTwo, Clock.Now());

            var order = CommandSchedulingTests.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(2).Date)
            {
                ShipmentId = clockOne
            });
            order.PendingEvents.Last().As<IHaveExtensibleMetada>().Metadata.ClockName = clockOne;
            orderRepository.Save(order);

            // act
            scheduler.AdvanceClock(clockName: clockTwo, by: TimeSpan.FromDays(30));

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<CommandScheduled<Order>>();
        }

        [Test]
        public async Task A_clock_cannot_be_moved_to_a_prior_time()
        {
            // arrange
            var name = Any.AlphanumericString(8, 8);
            scheduler.CreateClock(name, DateTimeOffset.UtcNow);

            // act
            Action moveBackwards = () => scheduler.AdvanceClock(name, DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(5))).Wait();

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
            var name = Any.AlphanumericString(8, 8);
            scheduler.CreateClock(name, DateTimeOffset.UtcNow);

            Action createAgain = () =>
                                 scheduler.CreateClock(name, DateTimeOffset.UtcNow.AddDays(1));

            createAgain.ShouldThrow<ConcurrencyException>()
                       .And
                       .Message
                       .Should()
                       .Contain(string.Format("A clock named '{0}' already exists", name));
        }

        [Test]
        public void A_clock_can_be_associated_with_an_aggregate_so_that_its_scheduled_commands_can_be_advanced_later()
        {
            // arrange
            var clockName = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();

            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());

            scheduler.AssociateWithClock(clockName, order.Id.ToString());

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(30)));
            orderRepository.Save(order);

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<CommandScheduled<Order>>();

            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(31));
            order = orderRepository.GetLatest(order.Id);
            lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public void When_an_association_is_made_with_an_existing_value_then_an_exception_is_thrown()
        {
            // arrange
            var clockName = Any.AlphanumericString(10, 10);
            var value = Any.FullName();
            scheduler.AssociateWithClock(clockName, value);

            // act
            Action reassociate = () => scheduler.AssociateWithClock(clockName + "-2", value);

            //assert 
            reassociate.ShouldThrow<InvalidOperationException>()
                       .And
                       .Message
                       .Should()
                       .Contain(string.Format("Value '{0}' is already associated with another clock", value));
        }

        [Test]
        public void When_a_scheduled_command_fails_and_the_clock_is_advanced_again_then_it_can_be_retried()
        {
            // ARRANGE
            var clockName = Any.AlphanumericString(8, 8);
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress
                {
                    NewEmailAddress = Any.Email()
                });
            var bus = new FakeEventBus();
            var repository = new SqlEventSourcedRepository<CustomerAccount>(bus);

            var configuration = new Configuration().UseSqlEventStore();
            configuration.Container.Register<IEventSourcedRepository<CustomerAccount>>(c => repository);
            var scheduler = new SqlCommandScheduler(configuration);
            scheduler.ClockLookupFor<CustomerAccount>(cmd => cmd.AggregateId.ToString());
            bus.Subscribe(scheduler);
            scheduler.AssociateWithClock(clockName, account.Id.ToString());
            account
                .Apply(new SendMarketingEmailOn(Clock.Now().AddDays(5)))
                .Apply(new RequestNoSpam());
            repository.Save(account);

            // ACT
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(6));
            account.CommunicationsSent.Count().Should().Be(0);

            // requesting spam will unblock the original scheduled command if it is re-attempted
            account = accountRepository.GetLatest(account.Id);
            account.Apply(new RequestSpam());
            repository.Save(account);
            scheduler.AdvanceClock(clockName, TimeSpan.FromMinutes(1));

            // ASSERT 
            account = repository.GetLatest(account.Id);
            account.CommunicationsSent.Count().Should().Be(1);
        }

        [Test]
        public void When_a_scheduled_command_fails_due_to_a_concurrency_exception_then_it_is_not_marked_as_applied()
        {
            // arrange 
            var clockName = Any.Paragraph(4);
            var order = CommandSchedulingTests.CreateOrder();
            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());
            scheduler.AssociateWithClock(clockName, order.Id.ToString());
            var ship = new Ship();
            Console.WriteLine(new { Ship = ship.ToJson() });

            order.Apply(ship);

            var chargeCreditCardOn = order.Apply(new ChargeCreditCardOn
            {
                Amount = 10,
                ChargeDate = Clock.Now().AddDays(10)
            });
            Console.WriteLine(new { ChargeCreditCardOn = chargeCreditCardOn.ToJson() });

            orderRepository.Save(order);

            orderRepository.GetEventStoreContext = () =>
            {
                // quick, add a new event in order to trigger a concurrency exception at the moment the scheduler tries to apply the command
                var repository = new SqlEventSourcedRepository<Order>();
                var o = repository.GetLatest(order.Id);
                o.Apply(new ChangeCustomerInfo { CustomerName = Any.FullName() });
                repository.Save(o);

                return new EventStoreDbContext();
            };

            // act
            scheduler.AdvanceClock(clockName, by: TimeSpan.FromDays(20));

            // assert
            using (var db = new CommandSchedulerDbContext())
            {
                // make sure we actually triggered a concurrency exception
                db.Errors
                  .Where(e => e.ScheduledCommand.AggregateId == order.Id)
                  .Should()
                  .Contain(e => e.Error.Contains("ConcurrencyException"));

                var scheduledCommand = db.ScheduledCommands.Single(c => c.AggregateId == order.Id);

                scheduledCommand.AppliedTime.Should().BeNull();
                scheduledCommand.Attempts.Should().Be(1);
            }
        }

        [Test]
        public void A_command_is_not_marked_as_applied_if_no_handler_is_registered()
        {
            // arrange
            var clockName = Any.Paragraph(4);
            var order = CommandSchedulingTests.CreateOrder();
            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());
            scheduler.AssociateWithClock(clockName, order.Id.ToString());
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(10)
                });
            orderRepository.Save(order);

            // act
            var schedulerWithNoHandlers = new SqlCommandScheduler(new Configuration().UseSqlEventStore());
            schedulerWithNoHandlers.AdvanceClock(clockName, by: TimeSpan.FromDays(20));

            // assert
            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands.Single(c => c.AggregateId == order.Id)
                  .AppliedTime
                  .Should()
                  .BeNull();
            }
        }

        [Test]
        public void When_a_scheduled_command_fails_then_the_error_is_recorded()
        {
            // arrange
            var clockName = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();
            var bus = new FakeEventBus();
            var innerRepository = new SqlEventSourcedRepository<Order>(bus);
            var saveCount = 0;
            var repository = new FakeRepository<Order>(innerRepository)
            {
                OnSave = o =>
                {
                    if (saveCount > 0)
                    {
                        throw new Exception("oops!");
                    }
                    innerRepository.Save(o);
                    saveCount++;
                }
            };
            var configuration = new Configuration().UseSqlEventStore();
            configuration.Container.Register<IEventSourcedRepository<Order>>(c => repository);
            var scheduler = new SqlCommandScheduler(configuration);
            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());
            bus.Subscribe(scheduler);
            scheduler.AssociateWithClock(clockName, order.Id.ToString());

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(30)));
            repository.Save(order);
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(31));

            //assert 
            using (var db = new CommandSchedulerDbContext())
            {
                var error = db.Errors.Single(c => c.ScheduledCommand.AggregateId == order.Id).Error;
                error.Should().Contain("oops!");
            }
        }

        [Test]
        public void All_commands_can_be_configured_for_SQL_scheduling_using_ScheduleCommandsUsingSqlScheduler()
        {
            var bus = new FakeEventBus();

            bus.Subscribe(new SqlCommandScheduler(new Configuration().UseSqlEventStore()));

            bus.SubscribedEventTypes()
               .Should()
               .Contain(new[]
               {
                   typeof (IScheduledCommand<Order>),
                   typeof (IScheduledCommand<CustomerAccount>)
               });
        }

        [Test]
        public void When_two_different_callers_advance_the_same_clock_at_the_same_time_then_commands_are_only_run_once()
        {
            // arrange
            var clockName = MethodBase.GetCurrentMethod().Name;
            var order = CommandSchedulingTests.CreateOrder();
            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());
            scheduler.AssociateWithClock(clockName, order.Id.ToString());
            var barrier = new Barrier(2);

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(5)));
            orderRepository.Save(order);
            var eventCount = order.Events().Count();
            scheduler.CreateCommandSchedulerDbContext = () =>
            {
                var commandSchedulerDbContext = new CommandSchedulerDbContext();
                barrier.SignalAndWait(TimeSpan.FromSeconds(10));
                return commandSchedulerDbContext;
            };
            var caller1 = Task.Run(() => scheduler.AdvanceClock(clockName, TimeSpan.FromDays(10)));
            var caller2 = Task.Run(() => scheduler.AdvanceClock(clockName, TimeSpan.FromDays(10)));

            try
            {
                Task.WaitAll(caller1, caller2);
            }
            catch (DataException ex)
            {
                Console.WriteLine("Expected exception thrown:\n" + ex);
            }

            orderRepository.GetLatest(order.Id)
                           .Events()
                           .Count()
                           .Should()
                           .Be(eventCount + 1, "the scheduled command should only be applied once");
        }

        [Test]
        public void The_aggregate_can_control_retries_of_a_failed_command()
        {
            // arrange
            var clockName = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();
            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());
            scheduler.AssociateWithClock(clockName, order.Id.ToString());
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(30)
                });
            orderRepository.Save(order);

            // act
            // initial attempt fails
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(31));
            order = orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // two more attempts
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(1));
            order = orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(1));
            order = orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // final attempt results in giving up
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(1));
            order = orderRepository.GetLatest(order.Id);
            var last = order.Events().Last();
            last.Should().BeOfType<Order.Cancelled>();
            last.As<Order.Cancelled>().Reason.Should().Be("Final credit card charge attempt failed.");
        }

        [Test]
        public void The_aggregate_can_retry_a_failed_command_as_soon_as_possible()
        {
            // arrange
            var clockName = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();
            scheduler.ClockLookupFor<Order>(cmd => cmd.AggregateId.ToString());
            scheduler.AssociateWithClock(clockName, order.Id.ToString());
            order.Apply(
                new ChargeCreditCardOn
                {
                    Amount = 10,
                    ChargeDate = Clock.Now().AddDays(30),
                    ChargeRetryPeriod = TimeSpan.Zero
                });
            orderRepository.Save(order);

            // act
            // initial attempt fails
            scheduler.AdvanceClock(clockName, TimeSpan.FromDays(31));
            order = orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // two more attempts, advancing the clock only a tick at a time
            scheduler.AdvanceClock(clockName, TimeSpan.FromTicks(1));
            order = orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();
            scheduler.AdvanceClock(clockName, TimeSpan.FromTicks(1));
            order = orderRepository.GetLatest(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // final attempt results in giving up
            scheduler.AdvanceClock(clockName, TimeSpan.FromTicks(1));
            order = orderRepository.GetLatest(order.Id);
            var last = order.Events().Last();
            last.Should().BeOfType<Order.Cancelled>();
            last.As<Order.Cancelled>().Reason.Should().Be("Final credit card charge attempt failed.");
        }


        [Test]
        public async Task The_aggregate_can_cancel_a_scheduled_command_after_it_fails()
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

            orderRepository.Save(order);

            Func<IQueryable<ScheduledCommand>, IQueryable<ScheduledCommand>> query;
            query = cmds =>
                    cmds.Where(cmd => cmd.AggregateId == order.Id)
                        .Where(cmd => cmd.AppliedTime == null &&
                                      cmd.FinalAttemptTime == null);

            var result = await scheduler.Trigger(query);

            // the command should fail validation 
            result.FailedCommands.Count().Should().Be(1);

            result = await scheduler.Trigger(query);

            result.FailedCommands.Count().Should().Be(0);
        }

        [Test]
        public void A_scheduled_command_can_schedule_other_commands()
        {
            // ARRANGE
            var clockName = Any.AlphanumericString(8, 8);
            var email = Any.Email();
            Console.WriteLine(new { clockName, email });
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress(email));
            scheduler.AssociateWithClock(clockName, account.Id.ToString());
            scheduler.ClockLookupFor<CustomerAccount>(cmd => cmd.AggregateId.ToString());
            account.Apply(new SendMarketingEmailOn(Clock.Now().AddDays(1)))
                   .Apply(new RequestSpam());
            accountRepository.Save(account);

            // ACT
            var result = scheduler.AdvanceClock(clockName, TimeSpan.FromDays(10));
            Console.WriteLine(result.ToLogString());

            // ASSERT
            account = accountRepository.GetLatest(account.Id);

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
        public async Task Specific_scheduled_commands_can_be_triggered_directly_by_aggregate_id()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var order = CommandSchedulingTests.CreateOrder();

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            });
            orderRepository.Save(order);

            // act
            await scheduler.Trigger(commands => commands.Where(cmd => cmd.AggregateId == order.Id));

            //assert 
            order = orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
            order.ShipmentId
                 .Should()
                 .Be(shipmentId);

            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == order.Id)
                  .Should()
                  .OnlyContain(c => c.AppliedTime != null);
            }
        }

        [Test]
        public async Task When_triggering_specific_commands_then_the_result_can_be_used_to_evaluate_failures()
        {
            // arrange
            var shipmentId = Any.AlphanumericString(8, 8);
            var customerAccountId = Any.Guid();
            accountRepository.Save(new CustomerAccount(customerAccountId)
                                       .Apply(new ChangeEmailAddress(Any.Email())));
            var order = CommandSchedulingTests.CreateOrder(customerAccountId: customerAccountId);

            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date)
            {
                ShipmentId = shipmentId
            })
                 .Apply(new Cancel());
            orderRepository.Save(order);

            // act
            var result = await scheduler.Trigger(commands => commands.Where(cmd => cmd.AggregateId == order.Id));

            //assert 
            order = orderRepository.GetLatest(order.Id);

            result.FailedCommands.Count().Should().Be(1);
            result.FailedCommands
                  .Single()
                  .Exception
                  .ToString()
                  .Should()
                  .Contain("The order has been cancelled");
        }

        [Test]
        public void Once_SqlCommandScheduler_is_resolved_from_the_Configuration_then_aggregate_specific_command_schedulers_are_registered()
        {
            var configuration = new Configuration().UseInMemoryEventStore();
            configuration.Container.Resolve<SqlCommandScheduler>();

            var scheduler = configuration.Container.Resolve<ICommandScheduler<Order>>();

            scheduler.Should()
                     .NotBeNull();
            configuration.Container
                         .Resolve<ICommandScheduler<Order>>()
                         .Should()
                         .BeSameAs(scheduler);
        }

        [Test]
        public async Task When_a_command_is_delivered_a_second_time_with_the_same_ETag_it_is_not_retried_afterward()
        {
            // arrange
            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = Any.Guid()
            });
            orderRepository.Save(order);

            var command = new AddItem
            {
                ProductName = Any.Word(),
                Price = 10m,
                ETag = Any.Guid().ToString()
            };

            var commandScheduler = Configuration.Current.Container.Resolve<ICommandScheduler<Order>>();

            // act
            await commandScheduler.Schedule(order.Id, command, Clock.Now().AddDays(1));
            Thread.Sleep(1); // the sequence number is set from the current tick count, which every now and then produces a duplicate here 
            await commandScheduler.Schedule(order.Id, command, Clock.Now().AddDays(1));
            await scheduler.Trigger(cmd => cmd.Where(c => c.AggregateId == order.Id));

            // assert
            order = orderRepository.GetLatest(order.Id);

            order.Balance.Should().Be(10);

            using (var db = new CommandSchedulerDbContext())
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
        public async Task When_a_command_is_scheduled_but_an_exception_is_thrown_in_a_handler_then_an_error_is_recorded()
        {
            var clockName = Any.CamelCaseName();
            scheduler.GetClockName = @event => clockName;

            Configuration.Current.UseDependency(_ => new CustomerAccount.OrderEmailConfirmer
            {
                SendOrderConfirmationEmail = async x => { throw new Exception("drat!"); }
            });

            // create a customer account
            var customer = new CustomerAccount(Any.Guid())
                .Apply(new ChangeEmailAddress(Any.Email()));
            accountRepository.Save(customer);

            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customer.Id
            });
            orderRepository.Save(order);

            // act
            order.Apply(new Cancel());
            orderRepository.Save(order);

            await scheduler.AdvanceClock(clockName, TimeSpan.FromMinutes(1.2));

            using (var db = new CommandSchedulerDbContext())
            {
                db.Errors
                  .Where(e => e.ScheduledCommand.AggregateId == customer.Id)
                  .Should()
                  .Contain(e => e.Error.Contains("drat!"));
            }
        }

        [Test]
        public void When_a_command_is_scheduled_but_the_event_that_triggered_it_was_not_successfully_written_then_the_command_is_not_applied()
        {
            VirtualClock.Start();

            // create a customer account
            var customer = new CustomerAccount(Any.Guid())
                .Apply(new ChangeEmailAddress(Any.Email()));
            accountRepository.Save(customer);

            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customer.Id
            });

            // act
            // cancel the order but don't save it
            order.Apply(new Cancel());

            // assert
            // verify that the customer did not receive the scheduled NotifyOrderCanceled command
            customer = accountRepository.GetLatest(customer.Id);
            customer.Events().Last().Should().BeOfType<CustomerAccount.EmailAddressChanged>();

            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == customer.Id)
                  .Should()
                  .ContainSingle(c => c.AppliedTime == null &&
                                      c.DueTime == null);
            }
        }

        [Test]
        public async Task When_a_command_is_scheduled_but_the_aggregate_it_applies_to_is_not_found_then_the_command_is_retried()
        {
            // arrange
            var clockName = Any.CamelCaseName();
            scheduler.GetClockName = e => clockName;

            // create and cancel an order for a nonexistent customer account 
            var customerId = Any.Guid();
            Console.WriteLine(new { customerId });
            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customerId
            });

            order.Apply(new Cancel());
            orderRepository.Save(order);

            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == customerId)
                  .Should()
                  .ContainSingle(c => c.AppliedTime == null);
            }

            // act
            // now save the customer and advance the clock
            accountRepository.Save(new CustomerAccount(customerId).Apply(new ChangeEmailAddress(Any.Email())));

            await scheduler.AdvanceClock(clockName, TimeSpan.FromMinutes(2));

            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Where(c => c.AggregateId == customerId)
                  .Should()
                  .ContainSingle(c => c.AppliedTime != null);

                var customer = accountRepository.GetLatest(customerId);
                customer.Events()
                        .Last()
                        .Should().BeOfType<CustomerAccount.OrderCancelationConfirmationEmailSent>();
            }
        }

        [Test]
        public async Task When_a_scheduler_assigned_negative_SequenceNumber_collides_then_it_retries_scheduling_with_a_new_SequenceNumber()
        {
            // arrange
            var container = Configuration.Current.Container;
            var scheduler = container.Resolve<ICommandScheduler<Order>>();

            var initialSequenceNumber = - Any.PositiveInt();

            var orderId = Any.Guid();

            await scheduler.Schedule(new CommandScheduled<Order>
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

            await scheduler.Schedule(secondCommand);

            using (var db = new CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                  .Count(c => c.AggregateId == orderId)
                  .Should()
                  .Be(2);
            }
        }

        [Test]
        public async Task When_a_non_scheduler_assigned_negative_SequenceNumber_collides_then_it_is_ignores()
        {
            // arrange
            var container = Configuration.Current.Container;
            var scheduler = container.Resolve<ICommandScheduler<Order>>();

            var db = new CommandSchedulerDbContext();

            disposables.Add(db);

            var initialSequenceNumber = Any.PositiveInt();

            var orderId = Any.Guid();

            await scheduler.Schedule(new CommandScheduled<Order>
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

            Action again = () => scheduler.Schedule(secondCommand).Wait();

            again.ShouldNotThrow<DbUpdateException>();
        }

        [Test]
        public async Task Constructor_commands_can_be_scheduled_to_create_new_aggregate_instances()
        {
            var scheduler = Configuration.Current.Container.Resolve<ICommandScheduler<Order>>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            await scheduler.Schedule(orderId, new CreateOrder(customerName)
            {
                AggregateId = orderId
            });

            var order = Configuration.Current.Container.Resolve<IEventSourcedRepository<Order>>().GetLatest(orderId);

            order.Should().NotBeNull();
            order.CustomerName.Should().Be(customerName);
        }

        [Test]
        public async Task When_an_immediately_scheduled_command_depends_on_an_event_that_has_not_been_saved_yet_then_there_is_not_initially_a_concurrency_exception()
        {
            var container = Configuration.Current.Container;
            var scheduler = container.Resolve<ICommandScheduler<Order>>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = Any.Guid(),
                ETag = Any.Guid().ToString()
            };

            var schedulerActivity = new List<ScheduledCommandResult>();
            using (container.Resolve<SqlCommandScheduler>()
                            .Activity
                            .Subscribe(schedulerActivity.Add))
            {
                await scheduler.Schedule(orderId,
                                         new CreateOrder(customerName)
                                         {
                                             AggregateId = orderId
                                         },
                                         deliveryDependsOn: prerequisiteEvent);
            }

            var order = orderRepository.GetLatest(orderId);

            order.Should().BeNull();

            using (var db = new CommandSchedulerDbContext())
            {
                var command = db.ScheduledCommands.Single(c => c.AggregateId == orderId);

                command.AppliedTime
                       .Should()
                       .BeNull();

                command.Attempts
                       .Should()
                       .Be(0);

                schedulerActivity
                    .Should()
                    .NotContain(a => a.IfTypeIs<ScheduledCommandFailure>()
                                   .Then(f => true)
                                   .ElseDefault());
            }
        }
        
        [Test]
        public async Task When_an_immediately_scheduled_command_depends_on_an_event_then_delivery_in_memory_waits_on_the_event_being_saved()
        {
            var container = Configuration.Current.Container;
            var scheduler = container.Resolve<ICommandScheduler<Order>>();

            var orderId = Any.Guid();
            var customerName = Any.FullName();

            var prerequisiteAggregateId = Any.Guid();
            var prerequisiteETag = Any.Guid().ToString();

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = prerequisiteAggregateId,
                ETag = prerequisiteETag
            };

            var schedulerActivity = new List<ScheduledCommandResult>();
            using (container.Resolve<SqlCommandScheduler>()
                            .Activity
                            .Subscribe(schedulerActivity.Add))
            {
                await scheduler.Schedule(orderId,
                                         new CreateOrder(customerName)
                                         {
                                             AggregateId = orderId
                                         },
                                         deliveryDependsOn: prerequisiteEvent);
            }

            // sanity check that the order is null
            var order = orderRepository.GetLatest(orderId);
            order.Should().BeNull();

            accountRepository.Save(new CustomerAccount(prerequisiteAggregateId)
                                       .Apply(new ChangeEmailAddress(Any.Email())
                                       {
                                           ETag = prerequisiteETag,
                                       }));

            // assert
            // now the order should have been created
            order = orderRepository.GetLatest(orderId);
            order.Should().NotBeNull();
        }

        [Test]
        public void UseSqlCommandScheduling_is_idempotent()
        {
            var configuration = new Configuration().UseSqlCommandScheduling();
            disposables.Add(configuration);

            var scheduler1 = configuration.Container.Resolve<SqlCommandScheduler>();
            var orderScheduler1 = configuration.Container.Resolve<ICommandScheduler<Order>>();

            configuration.UseSqlCommandScheduling();

            var scheduler2 = configuration.Container.Resolve<SqlCommandScheduler>();
            var orderScheduler2 = configuration.Container.Resolve<ICommandScheduler<Order>>();

            scheduler2.Should().BeSameAs(scheduler1);
            orderScheduler2.Should().BeSameAs(orderScheduler1);
        }

        [Test]
        public async Task UseSqlCommandScheduling_can_include_using_a_catchup_for_durability()
        {
            var configuration = new Configuration()
                .UseSqlEventStore()
                .UseSqlCommandScheduling(catchup => catchup.StartAtEventId = HighestEventId);
            disposables.Add(configuration);

            var aggregateIds = Enumerable.Range(1, 5).Select(_ => Any.Guid()).ToArray();

            Events.Write(5, i => new CommandScheduled<Order>
            {
                Command = new CreateOrder(Any.FullName()),
                AggregateId = aggregateIds[i - 1],
                SequenceNumber = -1
            });

            await configuration.Container
                               .Resolve<ReadModelCatchup<CommandSchedulerDbContext>>()
                               .SingleBatchAsync();

            using (var db = new CommandSchedulerDbContext())
            {
                var scheduledAggregateIds = db.ScheduledCommands
                                              .Where(c => aggregateIds.Any(id => id == c.AggregateId))
                                              .Select(c => c.AggregateId)
                                              .ToArray();

                scheduledAggregateIds.Should()
                                     .BeEquivalentTo(aggregateIds);
            }
        }

        [Test]
        public async Task When_a_command_is_non_durable_then_immediate_scheduling_does_not_result_in_command_scheduler_db_entry()
        {
            var commandScheduler = Configuration.Current.Container.Resolve<ICommandScheduler<CustomerAccount>>();
            var reserverationService = new Mock<IReservationService>();
            reserverationService.Setup(m => m.Reserve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                                .Returns(() => Task.FromResult(true));
            Configuration.Current.ReservationService = reserverationService.Object;

            var aggregateId = Any.Guid();
            accountRepository.Save(new CustomerAccount(aggregateId).Apply(new RequestNoSpam()));
            await commandScheduler.Schedule(aggregateId, new RequestUserName
                                                         {
                                                             UserName = Any.Email()
                                                         });

            using (var db = new CommandSchedulerDbContext())
            {
                var scheduledAggregateIds = db.ScheduledCommands
                                              .Where(c => aggregateId == c.AggregateId)
                                              .ToArray();

                scheduledAggregateIds.Should()
                                     .BeEmpty();
            }
        }

        public class FakeRepository<TAggregate> : IEventSourcedRepository<TAggregate> where TAggregate : class, IEventSourced
        {
            private readonly SqlEventSourcedRepository<TAggregate> innerRepository;

            public FakeRepository(SqlEventSourcedRepository<TAggregate> innerRepository)
            {
                this.innerRepository = innerRepository;
                OnSave = innerRepository.Save;
            }

            public TAggregate GetLatest(Guid aggregateId)
            {
                return innerRepository.GetLatest(aggregateId);
            }

            public TAggregate GetVersion(Guid aggregateId, long version)
            {
                return innerRepository.GetVersion(aggregateId, version);
            }

            public TAggregate GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
            {
                return innerRepository.GetAsOfDate(aggregateId, asOfDate);
            }

            public void Save(TAggregate aggregate)
            {
                OnSave(aggregate);
            }

            public void Refresh(TAggregate aggregate)
            {
                throw new NotImplementedException();
            }

            public Action<TAggregate> OnSave;
        }
    }
}
