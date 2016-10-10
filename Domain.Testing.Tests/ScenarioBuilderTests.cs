// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Its.Log.Instrumentation;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;
#pragma warning disable 618

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public abstract class ScenarioBuilderTests
    {
        private CompositeDisposable disposables;

        protected ScenarioBuilderTests()
        {
            Command<CustomerAccount>.AuthorizeDefault = (customer, command) => true;
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [SetUp]
        public virtual void SetUp()
        {
            disposables = new CompositeDisposable(
                Disposable.Create(() => ConfigurationContext.Current
                                          .IfNotNull()
                                          .ThenDo(c => c.Dispose())));
        }

        [TearDown]
        public virtual void TearDown()
        {
            disposables.Dispose();
        }

        protected void RegisterForDisposal(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        [Test]
        public void Events_added_to_the_scenario_are_used_to_source_aggregates()
        {
            var name = Any.FullName();
            var aggregateId = Any.Guid();
            var orderNumber = Any.Int(2000, 5000).ToString();

            var scenario = CreateScenarioBuilder()
                .AddEvents(new Order.Created
                {
                    AggregateId = aggregateId,
                    OrderNumber = orderNumber
                }, new Order.CustomerInfoChanged
                {
                    AggregateId = aggregateId,
                    CustomerName = name
                }).Prepare();

            var order = scenario.Aggregates.Single() as Order;

            order.Should().NotBeNull();
            order.OrderNumber.Should().Be(orderNumber);
            order.CustomerName.Should().Be(name);
            order.Id.Should().Be(aggregateId);
        }

        [Test]
        public void If_no_aggregate_id_is_specified_when_adding_events_then_a_default_is_chosen_and_reused()
        {
            var created = new Order.Created();
            var customerInfoChanged = new Order.CustomerInfoChanged();

            CreateScenarioBuilder()
                .AddEvents(created, customerInfoChanged);

            created.AggregateId.Should().NotBeEmpty();
            customerInfoChanged.AggregateId.Should().Be(created.AggregateId);
        }

        [Test]
        public async Task If_no_aggregate_id_is_specified_when_calling_GetLatest_and_a_single_instance_is_in_the_scenario_then_it_is_returned()
        {
            var aggregateId = Any.Guid();
            var created = new Order.Created
            {
                AggregateId = aggregateId
            };

            var scenario = CreateScenarioBuilder().AddEvents(created).Prepare();

            var aggregate = await scenario.GetLatestAsync<Order>();

            aggregate.Should().NotBeNull();
            aggregate.Id.Should().Be(aggregateId);
        }

        [Test]
        public void If_no_aggregate_id_is_specified_when_calling_GetLatest_and_no_instance_is_in_the_scenario_then_it_throws()
        {
            var scenario = CreateScenarioBuilder()
                .AddEvents(new Order.Created
                {
                    AggregateId = Any.Guid()
                }, new Order.Created
                {
                    AggregateId = Any.Guid()
                }).Prepare();

            Action getLatest = () => scenario.GetLatestAsync<Order>().Wait();

            getLatest.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void If_no_aggregate_id_is_specified_when_calling_GetLatest_and_multiple_instances_are_in_the_scenario_then_it_throws()
        {
            var scenario = CreateScenarioBuilder().Prepare();

            Action getLatest = () => scenario.GetLatestAsync<Order>().Wait();

            getLatest.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void When_no_sequence_numbers_are_specified_then_events_are_applied_in_order()
        {
            var aggregateId = Any.Guid();
            var firstCustomerName = Any.FullName();
            var scenario = CreateScenarioBuilder()
                .AddEvents(new Order.CustomerInfoChanged
                {
                    AggregateId = aggregateId,
                    CustomerName = Any.FullName()
                }, new Order.CustomerInfoChanged
                {
                    AggregateId = aggregateId,
                    CustomerName = firstCustomerName
                }).Prepare();

            var order = scenario.Aggregates.OfType<Order>().Single();

            order.CustomerName.Should().Be(firstCustomerName);
        }

        [Test]
        public void DynamicProjectors_registered_as_event_handlers_in_ScenarioBuilder_run_catchups_when_prepare_is_called()
        {
            var firstShipHandled = false;
            var secondShipHandled = false;
            var handler = Domain.Projector.CreateDynamic(dynamicEvent =>
            {
                var @event = dynamicEvent as IEvent;
                @event.IfTypeIs<CommandScheduled<Order>>()
                      .ThenDo(c =>
                      {
                          var shipmentId = (c.Command as Ship).ShipmentId;
                          Console.WriteLine("Handling [{0}] Shipment", shipmentId);
                          if (shipmentId == "first")
                          {
                              firstShipHandled = true;
                          }
                          else if (shipmentId == "second")
                          {
                              secondShipHandled = true;
                          }
                      });
            },
                                                         "Order.Scheduled:Ship");
            CreateScenarioBuilder()
                .AddHandler(handler)
                .AddEvents(
                    new CommandScheduled<Order>
                    {
                        Command = new Ship { ShipmentId = "first" },
                        DueTime = DateTime.Now
                    },
                    new CommandScheduled<Order>
                    {
                        Command = new Ship { ShipmentId = "second" },
                        DueTime = DateTime.Now
                    })
                .Prepare();

            firstShipHandled.Should().BeTrue();
            secondShipHandled.Should().BeTrue();
        }

        [Test]
        public void When_sequence_numbers_are_specified_it_overrides_the_order_in_which_the_events_were_added()
        {
            var aggregateId = Any.Guid();
            var firstCustomerName = Any.FullName();
            var scenario = CreateScenarioBuilder()
                .AddEvents(new Order.CustomerInfoChanged
                {
                    AggregateId = aggregateId,
                    CustomerName = firstCustomerName,
                    SequenceNumber = 2
                }, new Order.CustomerInfoChanged
                {
                    AggregateId = aggregateId,
                    CustomerName = Any.FullName(),
                    SequenceNumber = 1
                }).Prepare();

            var order = scenario.Aggregates.OfType<Order>().Single();

            order.CustomerName.Should().Be(firstCustomerName);
        }

        [Test]
        public void Multiple_aggregates_of_the_same_type_can_be_sourced_in_one_scenario()
        {
            var builder = CreateScenarioBuilder()
                .AddEvents(new Order.Created
                {
                    AggregateId = Any.Guid()
                }, new Order.Created
                {
                    AggregateId = Any.Guid()
                });

            builder.Prepare().Aggregates
                   .OfType<Order>()
                   .Count()
                   .Should().Be(2);
        }

        [Test]
        public void Multiple_aggregates_of_different_types_can_be_sourced_in_one_scenario()
        {
            // arrange
            var builder = CreateScenarioBuilder()
                .AddEvents(new Order.Created
                {
                    AggregateId = Any.Guid()
                }, new CustomerAccount.EmailAddressChanged
                {
                    AggregateId = Any.Guid()
                });

            // act
            var scenario = builder.Prepare();

            // assert
            scenario.Aggregates
                    .Should()
                    .ContainSingle(a => a is CustomerAccount)
                    .And
                    .ContainSingle(a => a is Order);
        }

        [Test]
        public void Multiple_aggregates_of_the_same_type_can_be_organized_using_For()
        {
            var aggregateId1 = Any.Guid();
            var aggregateId2 = Any.Guid();

            var builder = CreateScenarioBuilder();
            builder.For<Order>(aggregateId1)
                   .AddEvents(new Order.ItemAdded { ProductName = "one" });
            builder.For<Order>(aggregateId2)
                   .AddEvents(new Order.ItemAdded { ProductName = "two" });

            var aggregates = builder.Prepare().Aggregates.OfType<Order>().ToArray();

            aggregates.Count().Should().Be(2);
            aggregates.Should().Contain(a => a.Id == aggregateId1 && a.Items.Single().ProductName == "one");
            aggregates.Should().Contain(a => a.Id == aggregateId2 && a.Items.Single().ProductName == "two");
        }

        [Test]
        public void Projectors_added_before_Prepare_is_called_are_subscribed_to_all_events()
        {
            var onDeliveredCalls = 0;
            var onEmailAddedCalls = 0;
            var delivered = new Order.Delivered();
            var addressChanged = new CustomerAccount.EmailAddressChanged();
            CreateScenarioBuilder()
                .AddEvents(delivered, addressChanged)
                .AddHandler(new Projector
                {
                    OnDelivered = e =>
                    {
                        Console.WriteLine(e.ToLogString());
                        onDeliveredCalls++;
                    },
                    OnEmailAdded = e =>
                    {
                        Console.WriteLine(e.ToLogString());
                        onEmailAddedCalls++;
                    }
                })
                .Prepare();

            onDeliveredCalls.Should().Be(1);
            onEmailAddedCalls.Should().Be(1);
        }

        [Test]
        public async Task Projectors_added_after_Prepare_is_called_are_subscribed_to_future_events()
        {
            // arrange
            var onDeliveredCalls = 0;
            var onEmailAddedCalls = 0;
            var scenarioBuilder = CreateScenarioBuilder();
            var aggregateId = Any.Guid();
            var scenario = scenarioBuilder
                .AddEvents(new Order.Created
                {
                    AggregateId = aggregateId
                })
                .Prepare();

            scenarioBuilder.AddHandler(new Projector
            {
                OnDelivered = e => onDeliveredCalls++,
                OnEmailAdded = e => onEmailAddedCalls++
            });

            var order = new Order();
            order.Apply(new Deliver());
            var customer = new CustomerAccount();
            customer.Apply(new ChangeEmailAddress(Any.Email()));

            // act
            await scenario.SaveAsync(order);
            await scenario.SaveAsync(customer);

            // assert
            onDeliveredCalls.Should().Be(1);
            onEmailAddedCalls.Should().Be(1);
        }

        [Test]
        public void When_event_handling_errors_occur_during_Prepare_then_an_exception_is_thrown()
        {
            // arrange
            var scenarioBuilder = CreateScenarioBuilder();
            scenarioBuilder.AddEvents(new Order.Delivered());

            scenarioBuilder.AddHandler(new Projector
            {
                OnDelivered = e => { throw new Exception("oops!"); }
            });

            // act
            Action prepare = () => scenarioBuilder.Prepare();

            // assert
            prepare.ShouldThrow<ScenarioSetupException>()
                   .And
                   .Message
                   .Should()
                   .Contain("The following event handling errors occurred during projection catchup")
                   .And
                   .Contain("oops!");
        }

        [Test]
        public async Task When_event_handling_errors_occur_after_Prepare_then_they_can_be_verified_during_the_test()
        {
            // arrange
            var scenarioBuilder = CreateScenarioBuilder();
            var scenario = scenarioBuilder.AddHandler(new Projector
            {
                OnDelivered = e => { throw new Exception("oops!"); }
            }).Prepare();
            var order = new Order();
            order.Apply(new Deliver());
            await scenario.SaveAsync(order);

            // act
            Action verify = () => scenario.VerifyNoEventHandlingErrors();

            // assert
            verify.ShouldThrow<AssertionException>()
                  .And
                  .Message
                  .Should()
                  .Contain("The following event handling errors occurred")
                  .And
                  .Contain("oops!");
        }

        [Test]
        public void Consequenters_are_not_triggered_by_Prepare()
        {
            // arrange
            var onDeliveredCalls = 0;
            var scenarioBuilder = CreateScenarioBuilder();
            var aggregateId = Any.Guid();
            var builder = scenarioBuilder
                .AddEvents(
                    new Order.Created
                    {
                        AggregateId = aggregateId
                    },
                    new Order.Delivered())
                .AddHandler(new Consequenter
                {
                    OnDelivered = e => onDeliveredCalls++
                });

            // act
            builder.Prepare();

            // assert
            onDeliveredCalls.Should().Be(0);
        }

        [Test]
        public async Task Events_that_are_saved_as_a_result_of_commands_can_be_verified_using_the_EventBus()
        {
            // arrange
            var builder = CreateScenarioBuilder();
            var scenario = builder.Prepare();
            var customerName = Any.FullName();

            // act
            await scenario.SaveAsync(new Order(new CreateOrder(customerName)));

            // assert
            builder.EventBus
                   .PublishedEvents()
                   .OfType<Order.Created>()
                   .Should()
                   .ContainSingle(e => e.CustomerName == customerName);
        }

        [Test]
        public void AggregateBuilder_can_be_used_to_access_all_events_added_to_the_scenario_for_that_aggregate()
        {
            var scenarioBuilder = CreateScenarioBuilder();
            var orderId = Any.Guid();
            scenarioBuilder.For<Order>(orderId).AddEvents(new Order.Created());
            scenarioBuilder.AddEvents(new Order.ItemAdded
            {
                AggregateId = orderId
            });

            scenarioBuilder.For<Order>(orderId).InitialEvents.Count().Should().Be(2);
        }

        [Test]
        public void Event_timestamps_can_be_set_using_VirtualClock()
        {
            var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(Any.PositiveInt(10000)));

            using (VirtualClock.Start(startTime))
            {
                var scenario = CreateScenarioBuilder();

                scenario.AddEvents(new Order.Cancelled());

                scenario.InitialEvents.Last().Timestamp.Should().Be(startTime);
            }
        }

        [Test]
        public void Scenario_clock_can_be_advanced_by_a_specified_timespan()
        {
            var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(Any.PositiveInt(10000)));
            var timespan = TimeSpan.FromMinutes(Any.PositiveInt(1000));

            using (VirtualClock.Start(startTime))
            {
                var scenario = CreateScenarioBuilder()
                    .AdvanceClockTo(startTime)
                    .AdvanceClockBy(timespan);

                scenario.AddEvents(new Order.Cancelled());

                scenario.InitialEvents.Last().Timestamp.Should().Be(startTime + timespan);
            }
        }

        [Test]
        public void Scenario_clock_does_not_overwrite_specified_TimeStamp()
        {
            var time = Any.DateTimeOffset();
            ScenarioBuilder builder;

            using (VirtualClock.Start(time))
            {
                builder = CreateScenarioBuilder()
                    .AddEvents(new Order.ItemAdded
                    {
                        Timestamp = time
                    });
            }

            builder.InitialEvents.Single().Timestamp.Should().Be(time);
        }

        [Test]
        public void ScenarioBuilder_StartTime_returns_clock_time_when_there_are_no_initial_events()
        {
            var clockTime = Any.DateTimeOffset();
            using (VirtualClock.Start(clockTime))
            {
                var builder = CreateScenarioBuilder().AdvanceClockTo(clockTime);

                builder.StartTime().Should().Be(clockTime);
            }
        }

        [Test]
        public void ScenarioBuilder_StartTime_returns_earliest_event_timestamp_if_it_is_earlier_than_clock()
        {
            var eventTime = Any.DateTimeOffset();
            var clockTime = eventTime.Add(TimeSpan.FromDays(31));

            using (VirtualClock.Start(clockTime))
            {
                var builder = CreateScenarioBuilder()
                    .AdvanceClockTo(clockTime)
                    .AddEvents(new Order.ItemAdded
                    {
                        Timestamp = eventTime
                    });

                builder.StartTime().Should().Be(eventTime);
            }
        }

        [Test]
        public void ScenarioBuilder_StartTime_returns_clock_if_it_is_earlier_than_earliest_event_timestamp()
        {
            var clockTime = Any.DateTimeOffset();
            using (VirtualClock.Start(clockTime))
            {
                var eventTime = clockTime.Add(TimeSpan.FromMinutes(Any.PositiveInt(1000)));

                var builder = CreateScenarioBuilder()
                    .AddEvents(new Order.ItemAdded
                    {
                        Timestamp = eventTime
                    });

                builder.StartTime().Should().Be(clockTime);
            }
        }

        [Test]
        public async Task Scheduled_commands_in_initial_events_are_executed_if_they_become_due_after_Prepare_is_called()
        {
            using (VirtualClock.Start(Any.DateTimeOffset()))
            {
                var customerAccountId = Any.Guid();
                var scenario = CreateScenarioBuilder()
                    .AddEvents(
                        new CustomerAccount.Created
                        {
                            AggregateId= customerAccountId
                        },
                        new Order.Created
                        {
                            CustomerName = Any.FullName(),
                            OrderNumber = "42",
                            CustomerId = customerAccountId
                        },
                        new CommandScheduled<Order>
                        {
                            Command = new Cancel(),
                            DueTime = Clock.Now().AddDays(102)
                        }).Prepare();

                scenario.AdvanceClockBy(TimeSpan.FromDays(103));

                (await scenario.GetLatestAsync<Order>())
                        .EventHistory
                        .Last()
                        .Should()
                        .BeOfType<Order.Cancelled>();
            }
        }

        [Test]
        public async Task Recursive_scheduling_is_supported_when_the_scenario_clock_is_advanced()
        {
            // arrange
            using (VirtualClock.Start(DateTimeOffset.Parse("2014-10-08 06:52:10 AM -07:00")))
            {
                var scenario = CreateScenarioBuilder()
                    .AddEvents(new CustomerAccount.EmailAddressChanged
                    {
                        NewEmailAddress = Any.Email()
                    })
                    .Prepare();

                // act
                var account = (await scenario.GetLatestAsync<CustomerAccount>())
                                      .Apply(new RequestSpam())
                                      .Apply(new SendMarketingEmail());
                await scenario.SaveAsync(account);

                scenario.AdvanceClockBy(TimeSpan.FromDays(30));

                await scenario.CommandSchedulerDone();

                account = await scenario.GetLatestAsync<CustomerAccount>();

                // assert
                account.Events()
                       .OfType<CommandScheduled<CustomerAccount>>()
                       .Select(e => e.Timestamp.Date)
                       .Should()
                       .BeEquivalentTo(new[]
                       {
                           DateTime.Parse("2014-10-08"),
                           DateTime.Parse("2014-10-15"),
                           DateTime.Parse("2014-10-22"),
                           DateTime.Parse("2014-10-29"),
                           DateTime.Parse("2014-11-05")
                       });
                account.Events()
                       .OfType<CustomerAccount.MarketingEmailSent>()
                       .Select(e => e.Timestamp.Date)
                       .Should()
                       .BeEquivalentTo(new[]
                       {
                           DateTime.Parse("2014-10-08"),
                           DateTime.Parse("2014-10-15"),
                           DateTime.Parse("2014-10-22"),
                           DateTime.Parse("2014-10-29"),
                           DateTime.Parse("2014-11-05")
                       });
                account.Events()
                       .Last()
                       .Should()
                       .BeOfType<CommandScheduled<CustomerAccount>>();

                if (UsesSqlStorage)
                {
                    using (var db = Configuration.Current.CommandSchedulerDbContext())
                    {
                        var scheduledCommands = db.ScheduledCommands
                                                  .Where(c => c.AggregateId == account.Id)
                                                  .ToArray();

                        // all but the last command (which didn't come due yet) should have been marked as applied
                        scheduledCommands
                            .OrderByDescending(c => c.CreatedTime)
                            .Skip(1)
                            .Should()
                            .OnlyContain(c => c.AppliedTime != null);
                    }
                }
            }
        }

        public abstract bool UsesSqlStorage { get; }

        [Test]
        public async Task Recursive_scheduling_is_supported_when_the_virtual_clock_is_advanced()
        {
            // arrange
            using (VirtualClock.Start(DateTimeOffset.Parse("2014-10-08 06:52:10 AM -07:00")))
            {
                var scenario = CreateScenarioBuilder()
                    .AddEvents(new CustomerAccount.EmailAddressChanged
                    {
                        NewEmailAddress = Any.Email()
                    })
                    .Prepare();

                // act
                var account = (await scenario.GetLatestAsync<CustomerAccount>())
                                      .Apply(new RequestSpam())
                                      .Apply(new SendMarketingEmail());
                await scenario.SaveAsync(account);

                VirtualClock.Current.AdvanceBy(TimeSpan.FromDays((7*4) + 2));

                await scenario.CommandSchedulerDone();

                account = await scenario.GetLatestAsync<CustomerAccount>();

                // assert
                account.Events()
                       .OfType<CommandScheduled<CustomerAccount>>()
                       .Select(e => e.Timestamp.Date)
                       .Should()
                       .BeEquivalentTo(new[]
                       {
                           DateTime.Parse("2014-10-08"),
                           DateTime.Parse("2014-10-15"),
                           DateTime.Parse("2014-10-22"),
                           DateTime.Parse("2014-10-29"),
                           DateTime.Parse("2014-11-05")
                       });
                account.Events()
                       .OfType<CustomerAccount.MarketingEmailSent>()
                       .Select(e => e.Timestamp.Date)
                       .Should()
                       .BeEquivalentTo(new[]
                       {
                           DateTime.Parse("2014-10-08"),
                           DateTime.Parse("2014-10-15"),
                           DateTime.Parse("2014-10-22"),
                           DateTime.Parse("2014-10-29"),
                           DateTime.Parse("2014-11-05")
                       });
                account.Events()
                       .Last()
                       .Should()
                       .BeOfType<CommandScheduled<CustomerAccount>>();

                if (UsesSqlStorage)
                {
                    using (var db = Configuration.Current.CommandSchedulerDbContext())
                    {
                        var scheduledCommands = db.ScheduledCommands
                                                  .Where(c => c.AggregateId == account.Id)
                                                  .ToArray();

                        // all but the last command (which didn't come due yet) should have been marked as applied
                        scheduledCommands
                            .OrderByDescending(c => c.CreatedTime)
                            .Skip(1)
                            .Should()
                            .OnlyContain(c => c.AppliedTime != null);
                    }
                }
            }
        }

        [Test]
        public async Task Scheduled_commands_in_initial_events_are_not_executed_if_they_become_due_before_Prepare_is_called()
        {
            var aggregateId = Any.Guid();

            using (VirtualClock.Start())
            {
                var scenario = CreateScenarioBuilder()
                    .AddEvents(new CommandScheduled<Order>
                    {
                        AggregateId = aggregateId,
                        Command = new Cancel(),
                        DueTime = Clock.Now().AddDays(2)
                    })
                    .AdvanceClockBy(TimeSpan.FromDays(3))
                    .Prepare();

                (await scenario.GetLatestAsync<Order>(aggregateId))
                        .EventHistory
                        .Last()
                        .Should()
                        .BeOfType<CommandScheduled<Order>>();
            }
        }

        [Test]
        public async Task Handlers_can_be_added_by_type_and_are_instantiated_by_the_internal_container()
        {
            var customerId = Guid.NewGuid();
            var customerName = Any.FullName();
            var scenarioBuilder = CreateScenarioBuilder()
                .AddEvents(new CustomerAccount.UserNameAcquired
                {
                    AggregateId = customerId,
                    UserName = customerName
                });

            var scenario = scenarioBuilder.Prepare();
            scenarioBuilder.AddHandlers(typeof (SideEffectingConsequenter));

            var customerAccount = await scenario.GetLatestAsync<CustomerAccount>(customerId);
            customerAccount.Apply(new RequestNoSpam());
            await scenario.SaveAsync(customerAccount);

            await scenario.CommandSchedulerDone();

            var latest = await scenario.GetLatestAsync<CustomerAccount>(customerId);
            latest.EmailAddress.Should().Be("devnull@nowhere.com");
        }

        [Test]
        public void A_new_Scenario_can_be_prepared_while_another_is_still_active()
        {
            Configuration outerConfiguration = null;
            Configuration innerConfiguration = null;

            using (new ScenarioBuilder(c => { outerConfiguration = c; }).Prepare())
            using (new ScenarioBuilder(c => { innerConfiguration = c; }).Prepare())
            {
                Configuration.Current.Should().Be(innerConfiguration);
            }
        }

        protected abstract ScenarioBuilder CreateScenarioBuilder();

        public class Projector :
            IUpdateProjectionWhen<Order.Delivered>,
            IUpdateProjectionWhen<CustomerAccount.EmailAddressChanged>
        {
            public Action<Order.Delivered> OnDelivered = e => { };
            public Action<CustomerAccount.EmailAddressChanged> OnEmailAdded = e => { };

            public void UpdateProjection(Order.Delivered @event)
            {
                OnDelivered(@event);
            }

            public void UpdateProjection(CustomerAccount.EmailAddressChanged @event)
            {
                OnEmailAdded(@event);
            }
        }

        public class Consequenter :
            IHaveConsequencesWhen<Order.Delivered>
        {
            public Action<Order.Delivered> OnDelivered = e => { };

            public void HaveConsequences(Order.Delivered @event)
            {
                OnDelivered(@event);
            }
        }

        public class SideEffectingConsequenter :
            IHaveConsequencesWhen<CustomerAccount.RequestedNoSpam>
        {
            private readonly IEventSourcedRepository<CustomerAccount> customerRepository;

            public SideEffectingConsequenter(IEventSourcedRepository<CustomerAccount> customerRepository)
            {
                if (customerRepository == null)
                {
                    throw new ArgumentNullException("customerRepository");
                }
                this.customerRepository = customerRepository;
            }

            public void HaveConsequences(CustomerAccount.RequestedNoSpam @event)
            {
                var customer = customerRepository.GetLatest(@event.AggregateId).Result;
                customer.Apply(new ChangeEmailAddress
                {
                    NewEmailAddress = "devnull@nowhere.com"
                });
                customerRepository.Save(customer).Wait();
            }
        }
    }
}
