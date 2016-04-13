// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class InProcessEventBusTests
    {
        [Test]
        public void When_a_subscriber_subscribes_to_IEvent_then_they_receive_all_events()
        {
            var bus = new FakeEventBus();
            var events = new List<object>();

            bus.Events<IEvent>().Subscribe(events.Add);
            bus.PublishAsync(new Order.Cancelled()).Wait();

            events.Last().Should().BeOfType<Order.Cancelled>();
        }

        [Test]
        public void When_a_subscriber_subscribes_to_events_for_one_aggregate_then_they_do_not_receive_events_for_other_aggregates()
        {
            var bus = new FakeEventBus();
            var events = new List<object>();

            bus.Events<Event<Order>>().Subscribe(events.Add);
            bus.PublishAsync(new Order.Cancelled(), new TestAggregate.SomeEvent()).Wait();

            events.Last().Should().BeOfType<Order.Cancelled>();
        }

        [Test]
        public async Task If_Subscribe_is_called_more_than_once_for_a_given_handler_it_is_not_subscribed_again()
        {
            var callCount = 0;
            var handler = Projector.Create<Order.ItemAdded>(e => { callCount++; });

            var bus = new InProcessEventBus();

            bus.Subscribe(handler);
            bus.Subscribe(handler);

            await bus.PublishAsync(new Order.ItemAdded());

            callCount.Should().Be(1);
        }

        private class TestAggregate : IEventSourced
        {
            public class SomeEvent : Event<TestAggregate>
            {
                public override void Update(TestAggregate aggregate)
                {
                }
            }

            public Guid Id { get; private set; }

            public long Version
            {
                get
                {
                    return PendingEvents.Count();
                }
            }

            public IEnumerable<IEvent> PendingEvents { get; private set; }
            public void ConfirmSave()
            {
                throw new NotImplementedException();
            }
        }
    }
}
