// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using NUnit.Framework;
using Test.Domain.Ordering;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventTests
    {
        [Test]
        public void Event_KnownTypesForAggregateType_returns_known_event_types()
        {
            Event.KnownTypesForAggregateType(typeof (Order)).Should().Contain(new[]
            {
                typeof (Order.Cancelled),
                typeof (Order.CreditCardCharged),
                typeof (Order.CustomerInfoChanged),
                typeof (Order.Delivered),
                typeof (Order.Fulfilled),
                typeof (Order.FulfillmentMethodSelected),
                typeof (Order.ItemAdded),
                typeof (Order.ItemRemoved),
                typeof (Order.Misdelivered),
                typeof (Order.Paid),
                typeof (Order.Placed),
                typeof (Order.Shipped),
                typeof (Order.ShippingMethodSelected)
            });
        }

        [Test]
        public void EventOfT_KnownTypes_returns_known_event_types()
        {
            Event<Order>.KnownTypes.Should().Contain(new[]
            {
                typeof (Order.Cancelled),
                typeof (Order.CreditCardCharged),
                typeof (Order.CustomerInfoChanged),
                typeof (Order.Delivered),
                typeof (Order.Fulfilled),
                typeof (Order.FulfillmentMethodSelected),
                typeof (Order.ItemAdded),
                typeof (Order.ItemRemoved),
                typeof (Order.Misdelivered),
                typeof (Order.Paid),
                typeof (Order.Placed),
                typeof (Order.Shipped),
                typeof (Order.ShippingMethodSelected)
            });
        }

        [Test]
        public void EventName_returns_the_unqualified_type_name_for_non_generic_types()
        {
            new Order.ItemAdded().EventName().Should().Be("ItemAdded");
        }

        [Test]
        public void EventName_returns_the_type_name_and_generic_parameter_names_for_generic_types()
        {
            new TestEvent<Order>().EventName().Should().Be("TestEvent(Order)");
        }

        [Test]
        public void EventName_returns_the_display_name_for_types_attributed_with_EventNameAttribute()
        {
            new TestEventWithCustomName().EventName().Should().Be("Bob");
        }

        [Test]
        public void CommandScheduled_events_include_the_scheduled_command_name_in_the_event_name()
        {
            new CommandScheduled<Order>
            {
                Command = new Ship()
            }.EventName().Should().Be("Scheduled:Ship");
        }

        public class TestEvent<T> : Event<T> where T : IEventSourced
        {
            public override void Update(T aggregate)
            {
            }
        }

        [EventName("Bob")]
        public class TestEventWithCustomName : Event<Order> 
        {
            public string Value { get; set; }

            public override void Update(Order aggregate)
            {
            }
        }
    }
}
