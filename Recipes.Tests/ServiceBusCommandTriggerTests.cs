// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Its.Configuration;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.ServiceBus;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Tests;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using Clock = Microsoft.Its.Domain.Clock;

namespace Microsoft.Its.Cqrs.Recipes.Tests
{
    [Ignore("Integration tests")]
    [TestFixture, Category("Integration tests")]
    public class ServiceBusCommandTriggerTests : EventStoreDbTest
    {
        private CompositeDisposable disposables;
        private ServiceBusSettings serviceBusSettings;
        private ServiceBusCommandQueueSender queueSender;
        private List<IScheduledCommand<Order>> schedulerActivity;

        static ServiceBusCommandTriggerTests()
        {
            Formatter<ScheduledCommand>.RegisterForAllMembers();
            Formatter<ScheduledCommandResult>.RegisterForAllMembers();
            Formatter<CommandSucceeded>.RegisterForAllMembers();
            Formatter<CommandFailed>.RegisterForAllMembers();
        }

        [TestFixtureSetUp]
        public void Init()
        {
            Settings.Sources = new ISettingsSource[]
            {
                new ConfigDirectorySettings(@"c:\dev\.config")
            };
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            schedulerActivity = new List<IScheduledCommand<Order>>();

            using (VirtualClock.Start(DateTimeOffset.Now.AddMonths(1)))
            {
                disposables = new CompositeDisposable();

                serviceBusSettings = Settings.Get<ServiceBusSettings>();
                serviceBusSettings.NamePrefix = "itscqrstests";
                serviceBusSettings.ConfigureQueue = q => { q.AutoDeleteOnIdle = TimeSpan.FromMinutes(15); };

                var clockName = Any.Paragraph(4);

                var configuration = new Configuration()
                    .UseSqlEventStore()
                    .UseDependency<GetClockName>(_ => @event => clockName)
                    .UseSqlStorageForScheduledCommands()
                    .AddToCommandSchedulerPipeline<Order>(
                        schedule: async (cmd, next) =>
                        {
                            await next(cmd);
                            schedulerActivity.Add(cmd);
                        },
                        deliver: async (cmd, next) =>
                        {
                            await next(cmd);
                            schedulerActivity.Add(cmd);
                        });

                queueSender = new ServiceBusCommandQueueSender(serviceBusSettings)
                {
                    MessageDeliveryOffsetFromCommandDueTime = TimeSpan.FromSeconds(30)
                };

                disposables.Add(queueSender.Messages.Subscribe(s => Console.WriteLine("[ServiceBusCommandQueueSender] " + s.ToJson())));
                disposables.Add(configuration);
                disposables.Add(ConfigurationContext.Establish(configuration));
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
        public async Task When_ServiceBusCommandQueueSender_is_subscribed_to_the_service_bus_then_messages_are_scheduled_to_trigger_event_based_scheduled_commands()
        {
            VirtualClock.Start(DateTimeOffset.Now.AddHours(-13));

            using (var queueReceiver = CreateQueueReceiver())
            {
                var aggregateIds = Enumerable.Range(1, 5)
                                             .Select(_ => Guid.NewGuid())
                                             .ToArray();

                aggregateIds.ForEach(async id =>
                {
                    var order = CommandSchedulingTests_EventSourced.CreateOrder(orderId: id);

                    // due enough in the future that the scheduler won't apply the commands immediately
                    var due = Clock.Now().AddSeconds(5);
                    order.Apply(new ShipOn(due));

                    Console.WriteLine(new { ShipOrderId = order.Id, due });

                    await Configuration.Current.Repository<Order>().Save(order);
                });

                // reset the clock so that when the messages are delivered, the target commands are now due
                Clock.Reset();

                await queueReceiver.StartReceivingMessages();

                schedulerActivity
                    .Select(a => Guid.Parse(a.TargetId))
                    .ShouldBeEquivalentTo(aggregateIds);
            }
        }

        [Ignore("Test not finished")]
        [Test]
        public async Task When_ServiceBusCommandQueueSender_is_subscribed_to_the_service_bus_then_messages_are_scheduled_to_trigger_directly_scheduled_commands()
        {
            VirtualClock.Start(DateTimeOffset.Now.AddHours(-13));

            using (var queueReceiver = CreateQueueReceiver())
            {
                var aggregateIds = Enumerable.Range(1, 5)
                                             .Select(_ => Guid.NewGuid())
                                             .ToArray();

                aggregateIds.ForEach(id =>
                {
                    // TODO: (When_ServiceBusCommandQueueSender_is_subscribed_to_the_service_bus_then_messages_are_scheduled_to_trigger_directly_scheduled_commands) 
                });

                // reset the clock so that when the messages are delivered, the target commands are now due
                Clock.Reset();

                await queueReceiver.StartReceivingMessages();

                schedulerActivity
                    .Select(a => Guid.Parse(a.TargetId))
                    .ShouldBeEquivalentTo(aggregateIds);
            }
        }

        [Test]
        public async Task When_a_command_trigger_message_arrives_early_it_is_not_Completed()
        {
            VirtualClock.Start(Clock.Now().AddHours(-1));

            var aggregateId = Any.Guid();
            var appliedCommands = new List<ICommandSchedulerActivity>();

            using (var receiver = CreateQueueReceiver())
            {
                await receiver.StartReceivingMessages();

                // due enough in the future that the scheduler won't apply the commands immediately
                var order = await CommandSchedulingTests_EventSourced.CreateOrder(orderId: aggregateId)
                                                        .ApplyAsync(new ShipOn(Clock.Now().AddMinutes(2)));

                await Configuration.Current.Repository<Order>().Save(order);

                await receiver.Messages
                              .OfType<IScheduledCommand<Order>>()
                              .FirstAsync(c => c.TargetId == aggregateId.ToString())
                              .Timeout(TimeSpan.FromMinutes(1));

                await Task.Delay(1000);

                appliedCommands.Should().BeEmpty();
            }

            Clock.Reset();

            using (var receiver = CreateQueueReceiver())
            {
                await receiver.StartReceivingMessages();

                await Task.Delay(1000);

                // FIX: (When_a_command_trigger_message_arrives_early_it_is_not_Completed) how was this even passing?
//                appliedCommands.Should().Contain(c => c.ScheduledCommand.AggregateId == aggregateId);
            }
        }

        [Test]
        public async Task When_a_command_has_been_completed_and_a_message_for_it_arrives_the_message_is_also_Completed()
        {
            var aggregateId = Any.Guid();

            // due in the past so that it's scheduled immediately
            var order = CommandSchedulingTests_EventSourced.CreateOrder(orderId: aggregateId)
                                              .Apply(new ShipOn(Clock.Now().AddSeconds(-5)));
            queueSender.MessageDeliveryOffsetFromCommandDueTime = TimeSpan.FromSeconds(0);

            await Configuration.Current.Repository<Order>().Save(order);

            using (var receiver = CreateQueueReceiver())
            {
                var receivedMessages = new List<IScheduledCommand>();
                receiver.Messages
                        .Where(m => m.IfTypeIs<IScheduledCommand<Order>>()
                                     .Then(c => c.TargetId == aggregateId.ToString())
                                     .ElseDefault())
                        .Subscribe(receivedMessages.Add);

                await receiver.StartReceivingMessages();

                await Task.Delay(TimeSpan.FromSeconds(5));

                receivedMessages.Should()
                                .ContainSingle(m => m.IfTypeIs<IScheduledCommand<Order>>()
                                                     .Then(c => c.TargetId == aggregateId.ToString())
                                                     .ElseDefault());
            }

            using (var receiver = CreateQueueReceiver())
            {
                var receivedMessages = new List<IScheduledCommand>();

                receiver.Messages
                        .Where(m => m.IfTypeIs<IScheduledCommand<Order>>()
                                     .Then(c => c.TargetId == aggregateId.ToString())
                                     .ElseDefault())
                        .Subscribe(receivedMessages.Add);

                await receiver.StartReceivingMessages();

                await Task.Delay(TimeSpan.FromSeconds(10));

                receivedMessages.Count.Should().Be(0);
            }
        }

        private ServiceBusCommandQueueReceiver CreateQueueReceiver()
        {
            var receiver = new ServiceBusCommandQueueReceiver(
                serviceBusSettings,
                Configuration.Current.SchedulerClockTrigger(),
                () => new CommandSchedulerDbContext());

            receiver.Messages.Subscribe(s => Console.WriteLine("[ServiceBusCommandQueueReceiver] " + s.ToJson()));

            return receiver;
        }
    }
}