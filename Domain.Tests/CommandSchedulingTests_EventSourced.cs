// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Tests.CurrentConfiguration;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    [UseInMemoryCommandScheduling]
    [UseInMemoryEventStore]
    [DisableCommandAuthorization]
    public class CommandSchedulingTests_EventSourced
    {
        [Test]
        public async Task Aggregates_can_schedule_commands_against_themselves_idempotently()
        {
            var it = new MarcoPoloPlayerWhoIsIt();
            await Save(it);

            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.KeepSayingMarcoOverAndOver());

            VirtualClock.Current.AdvanceBy(TimeSpan.FromMinutes(1));

            it = await Get<MarcoPoloPlayerWhoIsIt>(it.Id);

            it.Events()
                .OfType<MarcoPoloPlayerWhoIsIt.SaidMarco>()
                .Count()
                .Should()
                .BeGreaterOrEqualTo(5);
        }

        [Test]
        public async Task CommandScheduler_executes_scheduled_commands_immediately_if_no_due_time_is_specified()
        {
            // arrange
            var order = CreateOrder();

            // act
            order.Apply(new ShipOn(Clock.Now().Subtract(TimeSpan.FromDays(2))));
            await Save(order);

            //assert 
            order = await Get<Order>(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        [Test]
        public void If_Schedule_is_dependent_on_an_event_with_no_aggregate_id_then_it_throws()
        {
            Action schedule = () => Schedule(
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
            var created = new Order.Created
            {
                AggregateId = Any.Guid(),
                ETag = null
            };

            await Schedule(
                Any.Guid(),
                new SendOrderConfirmationEmail(Any.Word()),
                deliveryDependsOn: created);

            created.ETag.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task Multiple_scheduled_commands_having_the_some_causative_command_etag_have_repeatable_and_unique_etags()
        {
            var scheduled = new List<ICommand>();
            var configuration = Configuration.Current;
            configuration.AddToCommandSchedulerPipeline<MarcoPoloPlayerWhoIsIt>(async (cmd, next) =>
            {
                scheduled.Add(cmd.Command);
                await next(cmd);
            });
            configuration.AddToCommandSchedulerPipeline<MarcoPoloPlayerWhoIsNotIt>(async (cmd, next) =>
            {
                scheduled.Add(cmd.Command);
                await next(cmd);
            });

            var it = new MarcoPoloPlayerWhoIsIt()
                .Apply(new MarcoPoloPlayerWhoIsIt.AddPlayer { PlayerId = Any.Guid() })
                .Apply(new MarcoPoloPlayerWhoIsIt.AddPlayer { PlayerId = Any.Guid() });

            await Save(it);

            var sourceEtag = Any.Guid().ToString();

            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.KeepSayingMarcoOverAndOver
            {
                ETag = sourceEtag
            });
            var firstPassEtags = scheduled.Select(c => c.ETag).ToArray();

            scheduled.Clear();

            // revert the aggregate and do the same thing again
            it = await Get<MarcoPoloPlayerWhoIsIt>(it.Id);
            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.KeepSayingMarcoOverAndOver
            {
                ETag = sourceEtag
            });

            var secondPassEtags = scheduled.Select(c => c.ETag).ToArray();

            secondPassEtags.Should()
                .Equal(firstPassEtags);
        }

        [Test]
        public async Task Scatter_gather_produces_a_unique_etag_per_sent_command()
        {
            var it = new MarcoPoloPlayerWhoIsIt();
            await Save(it);

            var numberOfPlayers = 6;
            var players = Enumerable.Range(1, numberOfPlayers)
                .Select(_ => new MarcoPoloPlayerWhoIsNotIt());

            foreach (var player in players)
            {
                var joinGame = new MarcoPoloPlayerWhoIsNotIt.JoinGame
                {
                    IdOfPlayerWhoIsIt = it.Id
                };
                await player.ApplyAsync(joinGame).AndSave();
            }

            it =  await Get<MarcoPoloPlayerWhoIsIt>(it.Id);

            await it.ApplyAsync(new MarcoPoloPlayerWhoIsIt.SayMarco()).AndSave();

            it =  await Get<MarcoPoloPlayerWhoIsIt>(it.Id);

            it.Events()
                .OfType<MarcoPoloPlayerWhoIsIt.HeardPolo>()
                .Count()
                .Should()
                .Be(numberOfPlayers);
        }

        [Test]
        public async Task Scheduled_commands_triggered_by_a_scheduled_command_are_idempotent()
        {
            var aggregate = new CommandSchedulerTestAggregate();

            await Save(aggregate);
          
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

            await Schedule(
                aggregate.Id,
                dueTime: dueTime,
                command: command);
            await Schedule(
                aggregate.Id,
                dueTime: dueTime,
                command: command);

            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(1));

            aggregate = await Get<CommandSchedulerTestAggregate>(aggregate.Id);

            var events = aggregate.Events().ToArray();
            events.Count().Should().Be(3);
            var succeededEvents = events.OfType<CommandSchedulerTestAggregate.CommandSucceeded>().ToArray();
            succeededEvents.Count().Should().Be(2);
            succeededEvents.First().Command.CommandId
                           .Should().NotBe(succeededEvents.Last().Command.CommandId);
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
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            // arrange
            var order = CreateOrder();

            // by the time Ship is applied, it will fail because of the cancellation
            order.Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));
            order.Apply(new Cancel());
            await Save(order);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            //assert 
            order = await Get<Order>(order.Id);
            var lastEvent = order.Events().Last();
            lastEvent.Should().BeOfType<Order.ShipmentCancelled>();
        }

        [Test]
        public async Task When_applying_a_scheduled_command_throws_then_further_command_scheduling_is_not_interrupted()
        {
            // arrange
            var customerAccountId = Any.Guid();
            var order1 = CreateOrder(customerAccountId: customerAccountId)
                .Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date))
                .Apply(new Cancel());
            await Save(order1);
            var order2 = CreateOrder(customerAccountId: customerAccountId)
                .Apply(new ShipOn(shipDate: Clock.Now().AddMonths(1).Date));
            await Save(order2);

            // act
            VirtualClock.Current.AdvanceBy(TimeSpan.FromDays(32));

            // assert 
            order1 = await Get<Order>(order1.Id);
            var lastEvent = order1.Events().Last();
            lastEvent.Should().BeOfType<Order.ShipmentCancelled>();

            order2 = await Get<Order>(order2.Id);
            lastEvent = order2.Events().Last();
            lastEvent.Should().BeOfType<Order.Shipped>();
        }

        public static Order CreateOrder(
            DateTimeOffset? deliveryBy = null,
            string customerName = null,
            Guid? orderId = null,
            Guid? customerAccountId = null)
        {
            return new Order(
                new CreateOrder(
                    orderId ?? Any.Guid(),
                    customerName ?? Any.FullName())
                {
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