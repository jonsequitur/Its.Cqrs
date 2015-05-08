// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class CommandContextTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [Test]
        public void The_event_Actor_is_set_from_the_Command_Principal()
        {
            // arrange
            var customer = new Customer(Any.FullName());
            var serviceRepresentative = new CustomerServiceRepresentative
            {
                Name = Any.FullName()
            };
            var command = new SpecifyShippingInfo
            {
                Principal = serviceRepresentative
            };

            var order = new Order(new CreateOrder(customer.Name)
            {
                Principal = customer
            });

            // act
            order.Apply(command);

            // assert
            order.Events()
                 .OfType<Order.Created>()
                 .Single()
                 .Actor()
                 .Should()
                 .Be(customer.Name);

            order.Events()
                 .OfType<Order.ShippingMethodSelected>()
                 .Single()
                 .Actor()
                 .Should()
                 .Be(serviceRepresentative.Name);
        }

        [Test]
        public void The_clock_set_in_the_CommandContext_is_used_by_resulting_events()
        {
            var created = DateTimeOffset.Parse("2014-05-15 00:00:00");

            var addItem = new AddItem
            {
                ProductName = "Widget",
                Price = 3.99m
            };

            Order order;
            using (CommandContext.Establish(addItem, Clock.Create(() => created)))
            {
                order = new Order(new CreateOrder(Any.FullName()));
                order.Apply(addItem);
            }

            order.Events()
                 .Count()
                 .Should()
                 .Be(3);
            order.Events()
                 .Should()
                 .OnlyContain(e => e.Timestamp == created);
        }

        [Test]
        public async Task When_one_command_triggers_another_command_via_a_consequenter_then_the_second_command_acquires_the_first_commands_clock()
        {
            // arrange
            var orderId = Any.Guid();
            var customerId = Any.Guid();
            var bus = new InProcessEventBus();
            var orderRepository = new InMemoryEventSourcedRepository<Order>(bus: bus);
            await orderRepository.Save(new Order(new CreateOrder(Any.FullName())
            {
                AggregateId = orderId,
                CustomerId = customerId
            }).Apply(new AddItem
            {
                ProductName = Any.Word(),
                Quantity = 1,
                Price = Any.Decimal(.01m, 10m)
            }));
            var customerRepository = new InMemoryEventSourcedRepository<CustomerAccount>();
            await customerRepository.Save(new CustomerAccount(customerId).Apply(new ChangeEmailAddress(Any.Email())));
            bus.Subscribe(Consequenter.Create<Order.Shipped>(e =>
            {
                var order = orderRepository.GetLatest(e.AggregateId).Result;
                var customer = customerRepository.GetLatest(order.CustomerId).Result;
                customer.Apply(new SendOrderConfirmationEmail(order.OrderNumber));
                customerRepository.Save(customer).Wait();
            }));
            var shipDate = DateTimeOffset.Parse("2014-05-15 01:01:01");
            var ship = new Ship();

            // act
            using (CommandContext.Establish(ship, Clock.Create(() => shipDate)))
            {
                var order = await orderRepository.GetLatest(orderId);
                order.Apply(ship);
                await orderRepository.Save(order);
            }

            // assert
            var last = (await customerRepository.GetLatest(customerId)).Events().Last();
            last.Should()
                .BeOfType<CustomerAccount.OrderShipConfirmationEmailSent>();
            last.Timestamp.Should().Be(shipDate);
        }

        [Test]
        public void When_one_command_triggers_another_command_within_EnactCommand_then_the_second_command_uses_the_CommandContext_clock()
        {
            var clockTime = DateTimeOffset.Parse("2014-05-13 09:28:42 AM");
            var shipOn = new ShipOn(DateTimeOffset.Parse("2014-06-01 00:00:00"));

            Order order;
            using (CommandContext.Establish(shipOn, Clock.Create(() => clockTime)))
            {
                order = new Order().Apply(shipOn);
            }

            order.Events()
                 .OfType<CommandScheduled<Order>>()
                 .Single()
                 .Timestamp
                 .Should()
                 .Be(clockTime);
        }

        [Test]
        public void When_CommandContexts_are_nested_then_the_later_clocks_time_is_used()
        {
            var earlierDate = DateTimeOffset.Parse("2014-05-14 00:00:00");
            var command1 = new Ship();
            var laterDate = DateTimeOffset.Parse("2014-05-15 00:00:00");
            var command2 = new Ship();

            using (CommandContext.Establish(command1, Clock.Create(() => earlierDate)))
            using (CommandContext.Establish(command2, Clock.Create(() => laterDate)))
            {
                CommandContext.Current.Clock.Now().Should().Be(laterDate);
            }
        }

        [Test]
        public void When_an_inner_context_has_a_later_date_than_an_outer_context_then_the_later_clock_remains_in_effect_after_inner_context_is_exited()
        {
            var earlierDate = DateTimeOffset.Parse("2014-05-14 00:00:00");
            var command1 = new Ship();
            var laterDate = DateTimeOffset.Parse("2014-05-15 00:00:00");
            var command2 = new Ship();

            using (CommandContext.Establish(command1, Clock.Create(() => earlierDate)))
            {
                using (CommandContext.Establish(command2, Clock.Create(() => laterDate)))
                {
                }
                CommandContext.Current.Clock.Now().Should().Be(laterDate);
            }
        }

        [Test]
        public void When_an_inner_context_has_an_earlier_date_than_an_outer_context_then_the_later_clock_remains_in_effect_after_inner_context_is_exited()
        {
            var earlierDate = DateTimeOffset.Parse("2014-05-14 00:00:00");
            var command1 = new Ship();
            var laterDate = DateTimeOffset.Parse("2014-05-15 00:00:00");
            var command2 = new Ship();

            using (CommandContext.Establish(command1, Clock.Create(() => laterDate)))
            {
                using (CommandContext.Establish(command2, Clock.Create(() => earlierDate)))
                {
                }
                CommandContext.Current.Clock.Now().Should().Be(laterDate);
            }
        }

        [Test]
        public void CommandContext_overrides_the_system_clock()
        {
            var beforeNow = DateTimeOffset.Parse("2013-05-14 00:00:00");
            var command = new Ship();

            using (CommandContext.Establish(command, Clock.Create(() => beforeNow)))
            {
                CommandContext.Current.Clock.Now().Should().Be(beforeNow);
            }
        }
    }
}
