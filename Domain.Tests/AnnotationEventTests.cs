// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class Given_an_aggregate_with_annotation_events
    {
        private readonly IEventSourcedRepository<Order> repository = new InMemoryEventSourcedRepository<Order>();

        private Guid aggregateId;
        private string customerName;

        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = delegate { return true; };

            customerName = Any.FullName();
            var order = new Order(new CreateOrder(customerName));
            repository.Save(order).Wait();
            aggregateId = order.Id;
        }

        [Test]
        public async Task The_aggregate_can_be_sourced()
        {
            var order = await repository.GetLatest(aggregateId);

            order.Apply(new Annotate<Order>(Any.String()));
            repository.Save(order).Wait();

            order = await repository.GetLatest(aggregateId);
            order.CustomerName.Should().Be(customerName);
        }

        [Test]
        public async Task The_annotated_event_is_deserialized_as_the_original_type()
        {
            var order = await repository.GetLatest(aggregateId);
            var message = Any.String();

            order.Apply(new Annotate<Order>(message));
            repository.Save(order).Wait();

            order = await repository.GetLatest(aggregateId);
            order.EventHistory.Should().ContainSingle(e => e is Annotated<Order>);
            order.EventHistory.OfType<Annotated<Order>>().Single().Message.Should().Be(message);
        }

        [Test]
        public async Task The_annotated_event_is_recorded_with_a_timestamp_which_reflects_actual_clock_time()
        {
            var actualNow = DateTimeOffset.Now;
            var virtualNow = DateTimeOffset.Parse("2000-01-01");
            VirtualClock.Start(virtualNow);

            var order = await repository.GetLatest(aggregateId);
            order.Apply(new Annotate<Order>("foo"));
            repository.Save(order).Wait();

            var @event = (await repository.GetLatest(aggregateId)).EventHistory.OfType<Annotated<Order>>().Single();
            @event.Timestamp.Should().NotBe(Clock.Now());
            @event.Timestamp.Should().BeAfter(actualNow);
        }
    }
}