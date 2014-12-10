using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Configuration;
using Microsoft.Its.Domain;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.ServiceBus;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Tests;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Log.Instrumentation;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using Clock = Microsoft.Its.Domain.Clock;

namespace Microsoft.Its.Cqrs.Recipes.Tests
{
    [Ignore("Integration tests")]
    [TestFixture, Category("Integration tests")]
    public class ServiceBusCommandTriggerTests : EventStoreDbTest
    {
        private SqlCommandScheduler scheduler;
        private FakeEventBus bus;
        private CompositeDisposable disposables;
        private SqlEventSourcedRepository<Order> orderRepository;
        private ServiceBusSettings serviceBusSettings;
        private ServiceBusCommandQueueSender queueSender;

        static ServiceBusCommandTriggerTests()
        {
            Formatter<ScheduledCommand>.RegisterForAllMembers();
            Formatter<ScheduledCommandResult>.RegisterForAllMembers();
            Formatter<ScheduledCommandFailure>.RegisterForAllMembers();
            Formatter<ScheduledCommandSuccess>.RegisterForAllMembers();
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            using (VirtualClock.Start(DateTimeOffset.Now.AddMonths(1)))
            {
                disposables = new CompositeDisposable();
                Settings.Sources = new ISettingsSource[] { new ConfigDirectorySettings(@"c:\dev\.config") }.Concat(Settings.Sources);

                serviceBusSettings = Settings.Get<ServiceBusSettings>();
                serviceBusSettings.NamePrefix = "itscqrstests";

                bus = new FakeEventBus();
                orderRepository = new SqlEventSourcedRepository<Order>(bus);

                var configuration = new Domain.Configuration()
                    .UseSqlEventStore(() => new EventStoreDbContext())
                    .UseEventBus(bus)
                    .UseDependency(_ => scheduler)
                    .UseDependency<IEventSourcedRepository<Order>>(t => orderRepository);

                var clockName = Any.Paragraph(4);
                scheduler = new SqlCommandScheduler(configuration) { GetClockName = @event => clockName };

                queueSender = new ServiceBusCommandQueueSender(serviceBusSettings)
                {
                    MessageDeliveryOffsetFromCommandDueTime = TimeSpan.FromSeconds(30)
                };

                disposables.Add(scheduler.Activity.Subscribe(s => Console.WriteLine("SqlCommandScheduler: " + s.ToJson())));
                disposables.Add(queueSender.Messages.Subscribe(s => Console.WriteLine("ServiceBusCommandQueueSender: " + s.ToJson())));
                disposables.Add(bus.Subscribe(scheduler));
                disposables.Add(configuration);
            }
        }

        [TearDown]
        public override void TearDown()
        {
            Settings.Reset();
            disposables.Dispose();
            Clock.Reset();
            base.TearDown();
        }

        [Test]
        public async Task When_ServiceBusCommandQueueSender_is_subscribed_to_the_service_bus_then_messages_are_scheduled_to_trigger_scheduled_commands()
        {
            VirtualClock.Start(DateTimeOffset.Now.AddHours(-13));

            using (var queueReceiver = CreateQueueReceiver())
            {
                var aggregateIds = Enumerable.Range(1, 5)
                                             .Select(_ => Guid.NewGuid())
                                             .ToArray();

                aggregateIds.ForEach(id =>
                {
                    var order = CommandSchedulingTests.CreateOrder(orderId: id);

                    // due enough in the future that the scheduler won't apply the commands immediately
                    var due = Clock.Now().AddSeconds(5);
                    order.Apply(new ShipOn(due));

                    Console.WriteLine(new { ShipOrderId = order.Id, due });

                    orderRepository.Save(order);
                });

                RunCatchup();

                // reset the clock so that when the messages are delivered, the target commands are now due
                Clock.Reset();

                queueReceiver.StartReceivingMessages();

                var activity = await scheduler.Activity
                                              .Where(a => aggregateIds.Contains(a.ScheduledCommand.AggregateId))
                                              .Take(5)
                                              .ToList()
                                              .Timeout(TimeSpan.FromMinutes(5));

                activity.Select(a => a.ScheduledCommand.AggregateId)
                        .ShouldBeEquivalentTo(aggregateIds);
            }
        }

        [Test]
        public async Task When_a_command_trigger_message_arrives_early_it_is_not_Completed()
        {
            VirtualClock.Start(Clock.Now().AddHours(-1));

            var aggregateId = Any.Guid();
            var appliedCommands = new List<ScheduledCommandResult>();

            scheduler.Activity.Where(c => c.ScheduledCommand.AggregateId == aggregateId)
                     .Subscribe(appliedCommands.Add);

            using (var receiver = CreateQueueReceiver())
            {
                receiver.StartReceivingMessages();

                // due enough in the future that the scheduler won't apply the commands immediately
                var order = CommandSchedulingTests.CreateOrder(orderId: aggregateId)
                                                  .Apply(new ShipOn(Clock.Now().AddSeconds(5)));
                orderRepository.Save(order);

                RunCatchup();

                await receiver.Messages
                              .FirstAsync(c => c.AggregateId == aggregateId)
                              .Timeout(TimeSpan.FromMinutes(1));

                appliedCommands.Should().BeEmpty();
            }

            Clock.Reset();

            using (var receiver = CreateQueueReceiver())
            {
                await receiver.StartReceivingMessages();

                Thread.Sleep(15);

                appliedCommands.Should().Contain(c => c.ScheduledCommand.AggregateId == aggregateId);
            }
        }

        [Test]
        public async Task When_a_command_has_been_completed_and_a_message_for_it_arrives_the_message_is_also_Completed()
        {
            var aggregateId = Any.Guid();

            // due in the past so that it's scheduled immediately
            var order = CommandSchedulingTests.CreateOrder(orderId: aggregateId)
                                              .Apply(new ShipOn(Clock.Now().AddSeconds(-5)));
            queueSender.MessageDeliveryOffsetFromCommandDueTime = TimeSpan.FromSeconds(0);

            orderRepository.Save(order);

            RunCatchup();

            using (var receiver = CreateQueueReceiver())
            {
                var receivedMessages = new List<IScheduledCommand>();
                receiver.Messages
                        .Where(c => c.AggregateId == aggregateId)
                        .Subscribe(receivedMessages.Add);

                await receiver.StartReceivingMessages();
                
                Thread.Sleep(TimeSpan.FromSeconds(5));

                receivedMessages.Should().ContainSingle(e => e.AggregateId == aggregateId);
            }

            using (var receiver = CreateQueueReceiver())
            {
                var receivedMessages = new List<IScheduledCommand>();

                receiver.Messages.Where(c => c.AggregateId == aggregateId)
                        .Subscribe(receivedMessages.Add);

                await receiver.StartReceivingMessages();

                Thread.Sleep(TimeSpan.FromSeconds(10));

                receivedMessages.Count().Should().Be(0);
            }
        }

        private ServiceBusCommandQueueReceiver CreateQueueReceiver()
        {
            var receiver = new ServiceBusCommandQueueReceiver(serviceBusSettings, scheduler);

            receiver.Messages.Subscribe(s => Console.WriteLine("ServiceBusCommandQueueReceiver: " + s.ToJson()));

            return receiver;
        }

        private void RunCatchup()
        {
            using (var catchup = CreateReadModelCatchup(queueSender))
            {
                catchup.CreateReadModelDbContext = () => new CommandSchedulerDbContext();
                catchup.Run();
            }
        }
    }
}