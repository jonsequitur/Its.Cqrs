// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using TraceListener = Its.Log.Instrumentation.TraceListener;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class CommandSchedulingTests
    {
        private CompositeDisposable disposables;
        private Configuration configuration;
        private Guid customerAccountId;
        private IEventSourcedRepository<CustomerAccount> customerRepository;
        private IEventSourcedRepository<Order> orderRepository;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable();
            // disable authorization
            Command<Order>.AuthorizeDefault = (o, c) => true;
            Command<CustomerAccount>.AuthorizeDefault = (o, c) => true;

            disposables.Add(VirtualClock.Start());

            customerAccountId = Any.Guid();

            configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .UseInMemoryEventStore()
                .TraceScheduledCommands();

            customerRepository = configuration.Repository<CustomerAccount>();
            orderRepository = configuration.Repository<Order>();

            customerRepository.Save(new CustomerAccount(customerAccountId)
                                        .Apply(new ChangeEmailAddress(Any.Email())));

            disposables.Add(ConfigurationContext.Establish(configuration));
            disposables.Add(configuration);
        }

        [TearDown]
        public void TearDown()
        {
            if (disposables != null)
            {
                disposables.Dispose();
            }
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
        public async Task CommandScheduler_executes_scheduled_commands_immediately_if_no_due_time_is_specified()
        {
            // arrange
            var repository = configuration.Repository<Order>();

            var order = CreateOrder();

            // act
            order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
            await repository.Save(order);

            //assert 
            order = await repository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            // arrange
            var order = CreateOrder(customerAccountId: (await customerRepository.GetLatest(customerAccountId)).Id);

            // by the time Ship is applied, it will fail because of the cancellation
            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));
            order.Apply(new Cancel());
            await orderRepository.Save(order);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            //assert 
            order = await orderRepository.GetLatest(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.ShipmentCancelled>();
        }

        [Test]
        public async Task When_applying_a_scheduled_command_throws_unexpectedly_then_further_command_scheduling_is_not_interrupted()
        {
            // arrange
            var order1 = CreateOrder(customerAccountId: customerAccountId)
                .Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date))
                .Apply(new Cancel());
            await orderRepository.Save(order1);
            var order2 = CreateOrder(customerAccountId: customerAccountId)
                .Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));
            await orderRepository.Save(order2);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            // assert 
            order1 = await orderRepository.GetLatest(order1.Id);
            var lastEvent = order1.Events().Last();
            lastEvent.Should().BeOfType<Order.ShipmentCancelled>();

            order2 = await orderRepository.GetLatest(order2.Id);
            lastEvent = order2.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public async Task A_command_can_be_scheduled_against_another_aggregate()
        {
            var order = new Order(
                new CreateOrder(Any.FullName())
                {
                    CustomerId = customerAccountId
                })
                .Apply(new AddItem
                {
                    ProductName = Any.Word(),
                    Price = 12.99m
                })
                .Apply(new Cancel());
            await orderRepository.Save(order);

            var customerAccount = await customerRepository.GetLatest(customerAccountId);

            customerAccount.Events()
                           .Last()
                           .Should()
                           .BeOfType<CustomerAccount.OrderCancelationConfirmationEmailSent>();
        }

        [Test]
        public void If_Schedule_is_dependent_on_an_event_with_no_aggregate_id_then_it_throws()
        {
            var scheduler = configuration.CommandScheduler<CustomerAccount>();

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
        public async Task If_Schedule_is_dependent_on_an_event_with_no_ETag_then_it_sets_one()
        {
            var scheduler = new Configuration()
                .UseInMemoryEventStore()
                .UseInMemoryCommandScheduling()
                .CommandScheduler<CustomerAccount>();

            var created = new Order.Created
            {
                AggregateId = Any.Guid(),
                ETag = null
            };

            await scheduler.Schedule(
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
                    schedule: async (cmd, next) => { scheduled = true; });

            var scheduler = configuration.CommandScheduler<Order>();

            await scheduler.Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            scheduled.Should().BeTrue();
        }

        [Test]
        public async Task CommandSchedulerPipeline_can_be_used_to_specify_command_scheduler_behavior_on_deliver()
        {
            var delivered = false;
            configuration
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

            configuration
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

            configuration
                .AddToCommandSchedulerPipeline<Order>(
                    schedule: async (cmd, next) =>
                    {
                        checkpoints.Add("one");
                        await next(cmd);
                        checkpoints.Add("four");
                    });

            // make sure to trigger a resolve
            var scheduler = configuration.CommandScheduler<Order>();

            configuration
                .AddToCommandSchedulerPipeline<Order>(
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
        public async Task When_CommandSchedulerPipeline_tracing_is_enabled_then_by_default_trace_output_goes_to_SystemDiagnosticsTrace()
        {
            configuration.TraceScheduledCommands();

            var log = new List<string>();
            using (LogTraceOutputTo(log))
            {
                await configuration.CommandScheduler<Order>()
                                   .Schedule(Any.Guid(), new CreateOrder(Any.FullName()));
            }

            log.Count.Should().Be(4);
            log.Should().Contain(e => e.Contains("[Scheduling]") &&
                                      e.Contains("Order.CreateOrder"));
            log.Should().Contain(e => e.Contains("[Scheduled]") &&
                                      e.Contains("Order.CreateOrder"));
            log.Should().Contain(e => e.Contains("[Delivering]") &&
                                      e.Contains("Order.CreateOrder"));
            log.Should().Contain(e => e.Contains("[Delivered]") &&
                                      e.Contains("Order.CreateOrder"));
        }

        [Test]
        public async Task CommandSchedulerPipeline_tracing_can_specify_tracing_behaviors()
        {
            var onSchedulingWasCalled = false;
            var onScheduledWasCalled = false;
            var onDeliveringWasCalled = false;
            var onDeliveredWasCalled = false;

            configuration.TraceScheduledCommands(
                onScheduling: _ => onSchedulingWasCalled = true,
                onScheduled: _ => onScheduledWasCalled = true,
                onDelivering: _ => onDeliveringWasCalled = true,
                onDelivered: _ => onDeliveredWasCalled = true);

            await configuration.CommandScheduler<Order>()
                               .Schedule(Any.Guid(), new CreateOrder(Any.FullName()));

            onSchedulingWasCalled.Should().BeTrue();
            onScheduledWasCalled.Should().BeTrue();
            onDeliveringWasCalled.Should().BeTrue();
            onDeliveredWasCalled.Should().BeTrue();
        }

        [Test]
        public async Task Scheduled_commands_triggered_by_a_scheduled_command_are_idempotent()
        {
            var aggregate = new CommandSchedulerTestAggregate();
            var repository = Configuration.Current
                                          .Repository<CommandSchedulerTestAggregate>();

            await repository.Save(aggregate);

            var scheduler = Configuration.Current.CommandScheduler<CommandSchedulerTestAggregate>();

            var dueTime = Clock.Now().AddMinutes(5);

            Console.WriteLine(new { dueTime });

            var command = new CommandSchedulerTestAggregate.CommandThatSchedulesTwoOtherCommandsImmediately
            {
                NextCommand1AggregateId = aggregate.Id,
                NextCommand1 = new CommandSchedulerTestAggregate.Command
                {
                    CommandId = Any.CamelCaseName()
                },
                NextCommand2AggregateId = aggregate.Id,
                NextCommand2 = new CommandSchedulerTestAggregate.Command
                {
                    CommandId = Any.CamelCaseName()
                }
            };

            await scheduler.Schedule(
                aggregate.Id,
                dueTime: dueTime,
                command: command);
            await scheduler.Schedule(
                aggregate.Id,
                dueTime: dueTime,
                command: command);

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            aggregate = await repository.GetLatest(aggregate.Id);

            var events = aggregate.Events().ToArray();
            events.Count().Should().Be(3);
            var succeededEvents = events.OfType<CommandSchedulerTestAggregate.CommandSucceeded>().ToArray();
            succeededEvents.Count().Should().Be(2);
            succeededEvents.First().Command.CommandId
                           .Should().NotBe(succeededEvents.Last().Command.CommandId);
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

        [Test]
        public async Task CommandSchedulerPipelineInitializer_Initialize_is_idempotent()
        {
            var commandsScheduled = new List<IScheduledCommand>();

            // initialize twice
            new AnonymousCommandSchedulerPipelineInitializer(cmd => commandsScheduled.Add(cmd))
                .Initialize(Configuration.Current);

            new AnonymousCommandSchedulerPipelineInitializer(cmd => commandsScheduled.Add(cmd))
                .Initialize(Configuration.Current);

            // send a command
            await Configuration.Current.CommandScheduler<Order>().Schedule(Any.Guid(), new CreateOrder(Any.FullName()));


            commandsScheduled.Count.Should().Be(1);
        }

        [Test]
        public async Task When_pipeline_tracing_is_enabled_twice_with_different_behavior_then_it_both_behaviors_are_applied()
        {
            var commandsScheduled = new List<IScheduledCommand>();
            var commandsDelivered = new List<IScheduledCommand>();
            configuration.TraceScheduledCommands()
                         .TraceScheduledCommands(onScheduled: async cmd => { commandsScheduled.Add(cmd); },
                                                 onDelivered: async cmd => { },
                                                 onScheduling: async cmd => { },
                                                 onDelivering: async cmd => { })
                         .TraceScheduledCommands(onDelivered: async cmd => { commandsDelivered.Add(cmd); },
                                                 onScheduled: async cmd => { },
                                                 onScheduling: async cmd => { },
                                                 onDelivering: async cmd => { });

            var log = new List<string>();
            using (LogTraceOutputTo(log))
            {
                // send a command
                await configuration.CommandScheduler<Order>().Schedule(Any.Guid(), new CreateOrder(Any.FullName()));
            }

            log.Count.Should().Be(4);
            commandsScheduled.Count.Should().Be(1);
            commandsDelivered.Count.Should().Be(1);
        }

        [Test]
        public async Task When_pipeline_tracing_is_enabled_multiple_times_with_default_behavior_then_it_does_not_produce_redundant_trace_output()
        {
            configuration.TraceScheduledCommands()
                         .TraceScheduledCommands()
                         .TraceScheduledCommands();

            var log = new List<string>();
            using (LogTraceOutputTo(log))
            {
                // send a command
                await configuration.CommandScheduler<Order>().Schedule(Any.Guid(), new CreateOrder(Any.FullName()));
            }

            log.Count.Should().Be(4);
        }

        public class AnonymousCommandSchedulerPipelineInitializer : CommandSchedulerPipelineInitializer
        {
            private readonly Action<IScheduledCommand> onSchedule;

            public AnonymousCommandSchedulerPipelineInitializer(Action<IScheduledCommand> onSchedule)
            {
                if (onSchedule == null)
                {
                    throw new ArgumentNullException("onSchedule");
                }
                this.onSchedule = onSchedule;
            }

            protected override void InitializeFor<TAggregate>(Configuration configuration)
            {
                configuration.AddToCommandSchedulerPipeline<TAggregate>(
                    schedule: async (cmd, next) =>
                    {
                        onSchedule(cmd);
                        await next(cmd);
                    });
            }

            public IEnumerable<IScheduledCommand> ScheduledCommands { get; private set; }

            public IEnumerable<IScheduledCommand> DeliveredCommands { get; private set; }
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

        public IDisposable LogTraceOutputTo(List<string> log)
        {
            configuration.TraceScheduledCommands();

            var listener = new TraceListener();
            Trace.Listeners.Add(listener);

            return new CompositeDisposable(Log.Events().Subscribe(e => log.Add(e.ToLogString())), Disposable.Create(() => Trace.Listeners.Remove(listener)));
        }
    }
}