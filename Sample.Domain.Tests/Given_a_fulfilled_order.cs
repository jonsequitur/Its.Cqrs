using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.DataMarket.Domain;
using Microsoft.DataMarket.Domain.Api.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Sample.Domain.Api.EventHandlers;
using Sample.Domain.Api.ReadModels;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using Microsoft.DataMarket.Domain.Tests;

namespace Sample.Domain.Tests
{
    [TestClass, TestFixture]
    public class Given_a_fulfilled_order
    {
        private Order order;
        private TestApi<Order> api;

        static Given_a_fulfilled_order()
        {
            Logging.Configure();
        }

        [SetUp, TestInitialize]
        public void SetUp()
        {
            // disable authorization checks
            Command<Order>.AuthorizeDefault = (o, c) => true;

            var events = new IEvent<Order>[]
            {
                new Order.ItemAdded { Price = 10m, Quantity = 2, ProductName = "Widget" },
                new Order.FulfillmentMethodSelected { FulfillmentMethod = FulfillmentMethod.Delivery },
                new Order.Placed(),
                new Order.Shipped(), 
                new Order.Paid(20),
                new Order.Delivered(),
                new Order.Fulfilled()
            };

            // set up an order to work with
            order = new Order(Guid.NewGuid(), events).SaveToEventStore();

            api = new TestApi<Order>();
        }

        [Test]
        public void when_attempting_to_cancel_the_order_it_throws()
        {
            order.Invoking(o => o.Apply(new Cancel()))
                 .ShouldThrow<CommandValidationException>();
        }

        [Test]
        public void when_attempting_to_change_the_fulfillment_method_it_throws()
        {
            order.Invoking(o => o.Apply(new ChangeFufillmentMethod()))
                 .ShouldThrow<CommandValidationException>();
        }

        [Test]
        public void a_history_read_model_should_be_available_reflecting_the_order_ship_date()
        {
            using (api.EventBus.Subscribe(new UpdateOrderHistory()))
            {
                api.EventBus.PublishAsync(order.EventHistory.ToArray()).Wait();

                using (var db = new OrderHistoryDbContext())
                {
                    var entry = db.Orders.Single(o => o.OrderId == order.Id);

                    var orderShippedOn = order.EventHistory.OfType<Order.Shipped>().Single().TimeStamp.Date;
                    var readModelShippedOn = entry.ShippedOn.Value.Date;
                    readModelShippedOn.Should().Be(orderShippedOn);
                }
            }
        }
    }
}