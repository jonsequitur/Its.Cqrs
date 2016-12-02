// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Infrastructure;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using Moq;
using NCrunch.Framework;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;
using static Microsoft.Its.Domain.Tests.CurrentConfiguration;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [NUnit.Framework.Category("Command scheduling")]
    [TestFixture]
    [UseSqlStorageForScheduledCommands]
    [UseSqlEventStore]
    [DisableCommandAuthorization]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerTests_EventSourced : SqlCommandSchedulerTests
    {
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
            await Save(order);

            // act
            await AdvanceClock(by: TimeSpan.FromDays(32));

            //assert 
            order = await Get<Order>(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
            order.ShipmentId
                .Should()
                .Be(shipmentId, "Properties should be transferred correctly from the serialized command");
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
            await Save(order);

            // act
            await AdvanceClock(by: TimeSpan.FromDays(30));

            //assert 
            order = await Get<Order>(order.Id);
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
            await Save(order);

            await SchedulerWorkComplete();

            //assert 
            order = await Get<Order>(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_scheduler_clock()
        {
            // arrange
            var order = CommandSchedulingTests_EventSourced.CreateOrder();

            await AdvanceClock(10.Days());

            // act
            order.Apply(new ShipOn(Clock.Now().AddDays(-5)));
            await Save(order);

            await SchedulerWorkComplete();

            //assert 
            order = await Get<Order>(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public override async Task Immediately_scheduled_commands_triggered_by_a_scheduled_command_have_their_due_time_set_to_the_causative_command_clock()
        {
            var aggregate = new CommandSchedulerTestAggregate();

            await Save(aggregate);


            var dueTime = Clock.Now().AddMinutes(5);

            await Schedule(
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

            using (var db = CommandSchedulerDbContext())
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
        public override async Task Scheduled_commands_with_no_due_time_are_delivered_at_Clock_Now_when_delivery_is_deferred()
        {
            // arrange
            var deliveredTime = new DateTimeOffset();

            Configuration
                .Current
                .UseCommandHandler<Order, CreateOrder>(async (_, __) => deliveredTime = Clock.Now());

            // act
            await Schedule(
                new CreateOrder(Any.FullName())
                {
                    CanBeDeliveredDuringScheduling = false
                },
                dueTime: null);

            await AdvanceClock(clockName: clockName, by: 1.Hours());

            // assert 
            deliveredTime
                .Should()
                .Be(Clock.Now());
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
            await Save(order);
            await AdvanceClock(TimeSpan.FromDays(31));

            //assert 
            using (var db = CommandSchedulerDbContext())
            {
                var error = db.Errors.Single(c => c.ScheduledCommand.AggregateId == order.Id).Error;
                error.Should().Contain("oops!");
            }
        }

        [Test]
        public async Task When_a_scheduled_command_fails_then_the_events_written_by_the_command_handler_are_not_saved_to_the_aggregate()
        {
            var aggregate = new CommandSchedulerTestAggregate();

            await Save(aggregate);

            await Schedule(
                aggregate.Id,
                command: new CommandSchedulerTestAggregate.CommandThatRecordsCommandSucceededEventWithoutExplicitlySavingAndThenFails());

            var latestAggregate = await Get<CommandSchedulerTestAggregate>(aggregate.Id);

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
            await Save(order);

            // act
            // initial attempt fails
            await AdvanceClock(TimeSpan.FromDays(31));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // two more attempts
            await AdvanceClock(TimeSpan.FromDays(1));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();
            await AdvanceClock(TimeSpan.FromDays(1));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // final attempt results in giving up
            await AdvanceClock(TimeSpan.FromDays(1));
            order = await Get<Order>(order.Id);
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
            await Save(order);

            // act
            // initial attempt fails
            await AdvanceClock(TimeSpan.FromDays(30.1));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // two more attempts, advancing the clock only a tick at a time
            await AdvanceClock(TimeSpan.FromTicks(1));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();
            await AdvanceClock(TimeSpan.FromTicks(1));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // final attempt results in giving up
            await AdvanceClock(TimeSpan.FromTicks(1));
            order = await Get<Order>(order.Id);
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
            await Save(order);

            // act
            // initial attempt fails
            await AdvanceClock(TimeSpan.FromDays(4));
            order = await Get<Order>(order.Id);
            order.Events().Last().Should().BeOfType<CommandScheduled<Order>>();

            // ship the order so the next retry will succeed
            order.Apply(new Ship());
            await Save(order);

            // advance the clock a couple of days, which doesn't trigger a retry yet
            await AdvanceClock(TimeSpan.FromDays(2));
            order = await Get<Order>(order.Id);
            order.Events().Should().NotContain(e => e is Order.CreditCardCharged);

            // advance the clock enough to trigger a retry
            await AdvanceClock(TimeSpan.FromDays(5));
            order = await Get<Order>(order.Id);
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

            await Save(order);

            var result = await AdvanceClock(11.Days());

            // the command should fail validation 
            result.FailedCommands.Count().Should().Be(1);

            result = await AdvanceClock(1.Hours());

            result.FailedCommands.Count().Should().Be(0);
        }

        [Test]
        public async Task When_a_command_is_triggered_and_succeeds_then_Result_SuccessfulCommands_references_it()
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
            var result = await AdvanceClock(by: 32.Days());

            //assert 
            result.SuccessfulCommands
                  .Should()
                  .ContainSingle(a => a.ScheduledCommand
                                       .IfTypeIs<IScheduledCommand<Order>>()
                                       .Then(c => c.TargetId == order.Id.ToString())
                                       .ElseDefault());
        }

        [Test]
        public async Task When_a_command_is_delivered_a_second_time_with_the_same_ETag_it_is_not_retried_afterward()
        {
            // arrange
            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = Any.Guid()
            });
            await Save(order);

            var command = new AddItem
            {
                ProductName = Any.Word(),
                Price = 10m,
                ETag = Any.Guid().ToString()
            };

            // act
            var scheduledCommand = await Schedule(order.Id, command, Clock.Now().AddDays(1));

            var deliverer = Configuration
                .Current
                .CommandDeliverer<Order>();

            await deliverer.Deliver(scheduledCommand);
            await deliverer.Deliver(scheduledCommand);
    
            // assert
            order = await Get<Order>(order.Id);

            order.Balance.Should().Be(10);

            using (var db = CommandSchedulerDbContext())
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
            await Save(customer);

            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customer.Id
            });
            await Save(order);

            // act
            order.Apply(new Cancel());
            await Save(order);

            await AdvanceClock(TimeSpan.FromMinutes(1.2));

            using (var db = CommandSchedulerDbContext())
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
            // create a customer account
            var customer = new CustomerAccount(Any.Guid())
                .Apply(new ChangeEmailAddress(Any.Email()));
            await Save(customer);

            var order = new Order(new CreateOrder(Any.FullName())
            {
                CustomerId = customer.Id
            });

            // act
            // cancel the order but don't save it
            order.Apply(new Cancel());

            // assert
            // verify that the customer did not receive the scheduled NotifyOrderCanceled command
            customer = await Get<CustomerAccount>(customer.Id);
            customer.Events().Last().Should().BeOfType<CustomerAccount.EmailAddressChanged>();

            using (var db = CommandSchedulerDbContext())
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
            await Save(order);

            using (var db = CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                    .Where(c => c.AggregateId == customerId)
                    .Should()
                    .ContainSingle(c => c.AppliedTime == null);
            }

            // act
            // now save the customer and advance the clock
            await Save(new CustomerAccount(customerId).Apply(new ChangeEmailAddress(Any.Email())));

            await AdvanceClock(TimeSpan.FromMinutes(2));

            using (var db = CommandSchedulerDbContext())
            {
                db.ScheduledCommands
                    .Where(c => c.AggregateId == customerId)
                    .Should()
                    .ContainSingle(c => c.AppliedTime != null);

                var customer = await Get<CustomerAccount>(customerId);
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

            using (var db = CommandSchedulerDbContext())
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

            // act
            Action again = () => commandScheduler.Schedule(secondCommand).Wait();

            // assert
            again.ShouldNotThrow<DbUpdateException>();
        }

        [Test]
        public override async Task Constructor_commands_can_be_scheduled_to_create_new_aggregate_instances()
        {
            var orderId = Any.Guid();
            var customerName = Any.FullName();

            await Schedule(orderId, new CreateOrder(orderId, customerName));

            var order = await Get<Order>(orderId);

            order.Should().NotBeNull();
            order.CustomerName.Should().Be(customerName);
        }

        [Test]
        public override async Task When_a_constructor_command_fails_with_a_ConcurrencyException_it_is_not_retried()
        {
            var orderId = Any.Guid();
            var customerName = Any.FullName();

            await Schedule(orderId, new CreateOrder(orderId, customerName));

            await Schedule(orderId, new CreateOrder(orderId, customerName));

            using (var db = CommandSchedulerDbContext())
            {
                var commands = db.ScheduledCommands
                    .Where(c => c.AggregateId == orderId)
                    .OrderBy(c => c.CreatedTime)
                    .ToArray();

                commands.Length.Should().Be(2);
                commands
                    .Should()
                    .ContainSingle(c => c.FinalAttemptTime == null);
            }
        }

        [Test]
        public override async Task When_an_immediately_scheduled_command_depends_on_a_precondition_that_has_not_been_met_yet_then_there_is_not_initially_an_attempt_recorded()
        {
            var orderId = Any.Guid();
            var customerName = Any.FullName();

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = Any.Guid(),
                ETag = Any.Guid().ToString()
            };

            var scheduledCommand = await Configuration
                                             .Current
                                             .CommandScheduler<Order>()
                                             .Schedule(orderId,
                                                 new CreateOrder(orderId, customerName),
                                                 deliveryDependsOn: prerequisiteEvent);

            var order = await Get<Order>(orderId);

            order.Should().BeNull();

            scheduledCommand.Result.Should().BeOfType<CommandScheduled>();

            using (var db = CommandSchedulerDbContext())
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
            var orderId = Any.Guid();
            var customerName = Any.FullName();

            var prerequisiteAggregateId = Any.Guid();
            var prerequisiteETag = Any.Guid().ToString();

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = prerequisiteAggregateId,
                ETag = prerequisiteETag
            };

            await Schedule(orderId,
                new CreateOrder(orderId, customerName),
                deliveryDependsOn: prerequisiteEvent);

            // sanity check that the order is null
            var order = await Get<Order>(orderId);
            order.Should().BeNull();

            await Save(new CustomerAccount(prerequisiteAggregateId)
                .Apply(new ChangeEmailAddress(Any.Email())
                {
                    ETag = prerequisiteETag
                }));

            await SchedulerWorkComplete();

            // assert
            // now the order should have been created
            order = await Get<Order>(orderId);
            order.Should().NotBeNull();
        }

        [Test]
        public override async Task When_a_scheduled_command_depends_on_an_event_that_never_arrives_it_is_eventually_abandoned()
        {
            var orderId = Any.Guid();
            await Save(new Order(new CreateOrder(orderId, Any.FullName())));

            var prerequisiteEvent = new CustomerAccount.Created
            {
                AggregateId = Any.Guid(),
                ETag = Any.Guid().ToString()
            };

            await Schedule(orderId,
                new AddItem
                {
                    ProductName = Any.Paragraph(3),
                    Price = 10m
                },
                deliveryDependsOn: prerequisiteEvent);

            for (var i = 0; i < 7; i++)
            {
                await AdvanceClock(by: 90.Days());
            }

            using (var db = CommandSchedulerDbContext())
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

            var orderId = Any.Guid();

            Configuration.Current
                .AddToCommandSchedulerPipeline<Order>(
                    schedule:
                        async (c, next) =>
                        {
                            scheduleCount++;
                            await next(c);
                        },
                    deliver:
                        async (c, next) =>
                        {
                            deliverCount++;
                            await next(c);
                        });

            await Schedule(
                dueTime: Clock.Now().AddDays(-1),
                aggregateId: orderId,
                command: new CreateOrder(orderId, Any.FullName()));

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            scheduleCount.Should().Be(1);
            deliverCount.Should().Be(1);
        }

        [Test]
        public async Task When_a_command_is_non_durable_then_immediate_scheduling_does_not_result_in_a_command_scheduler_db_entry()
        {
            var reserverationService = new Mock<IReservationService>();
            reserverationService.Setup(m => m.Reserve(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                .Returns(() => Task.FromResult(true));
            Configuration.Current.UseDependency(_ => reserverationService.Object);

            var aggregateId = Any.Guid();
            await Save(new CustomerAccount(aggregateId).Apply(new RequestNoSpam()));
            await Schedule(aggregateId, new RequestUserName
            {
                UserName = Any.Email()
            });

            using (var db = CommandSchedulerDbContext())
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
            var failedAggregateId = Any.Guid();
            var successfulAggregateId = Any.Guid();

            await Schedule(
                new CreateOrder(failedAggregateId, Any.FullName()),
                Clock.Now().AddHours(1));
            await Schedule(
                new CreateOrder(successfulAggregateId, Any.FullName()),
                Clock.Now().AddHours(1.5));

            using (var db = CommandSchedulerDbContext())
            {
                var command = db.ScheduledCommands.Single(c => c.AggregateId == failedAggregateId);
                var commandBody = command.SerializedCommand.FromJsonTo<dynamic>();
                commandBody.Command.CustomerId = "not a guid";
                command.SerializedCommand = commandBody.ToString();
                db.SaveChanges();
            }

            // act
            Action advanceClock = () => AdvanceClock(by: TimeSpan.FromHours(2)).Wait();

            // assert
            advanceClock.ShouldNotThrow();

            var successfulAggregate = await Get<Order>(successfulAggregateId);
            successfulAggregate.Should().NotBeNull();
        }

        [Test]
        public override async Task When_a_clock_is_set_on_a_command_then_it_takes_precedence_over_default_clock()
        {
            // arrange
            var clockName = Any.CamelCaseName();
            var create = new CreateOrder(Any.Guid(), Any.FullName());

            var clock = new CommandScheduler.Clock
            {
                Name = clockName,
                UtcNow = DateTimeOffset.Parse("2016-03-01 02:00:00 AM")
            };

            using (var commandScheduler = CommandSchedulerDbContext())
            {
                commandScheduler.Clocks.Add(clock);
                commandScheduler.SaveChanges();
            }

            // act
            await Schedule(
                create,
                dueTime: DateTimeOffset.Parse("2016-03-20 09:00:00 AM"),
                clock: clock);

            await AdvanceClock(clockName: clockName, by: 30.Days());

            //assert 
            var target = await Get<Order>(create.AggregateId);

            target.Should().NotBeNull();
        }
    }
}