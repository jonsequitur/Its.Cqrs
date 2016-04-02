// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Test.Domain.Ordering;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventHandlingTests
    {
        [Test]
        public void Handler_chains_can_be_specified_for_all_event_types_on_a_single_projector()
        {
            // arrange
            var bus = new InProcessEventBus();
            var createdWasCalled = false;
            var cancelledWasCalled = false;
            var deliveredWasCalled = false;

            var handledEvents = new List<IEvent>();

            // act
            var handler = new TestProjector(
                onCreated: e => createdWasCalled = true,
                onCancelled: e => cancelledWasCalled = true,
                onDelivered: e => deliveredWasCalled = true)
                .WrapAll((e, nextHandler) =>
                {
                    handledEvents.Add(e);
                    nextHandler(e);
                });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();
            bus.PublishAsync(new Order.Cancelled()).Wait();
            bus.PublishAsync(new Order.Delivered()).Wait();

            // assert
            handledEvents.Count.Should().Be(3);
            createdWasCalled.Should().BeTrue("created was called");
            cancelledWasCalled.Should().BeTrue("cancelled was called");
            deliveredWasCalled.Should().BeTrue("delivered was called");
        }

        [Test]
        public void When_multiple_projector_handlers_are_chained_then_the_last_added_is_called_first()
        {
            // arrange
            var bus = new InProcessEventBus();

            var handlerCalls = new List<string>();

            // act
            var handler = new TestProjector()
                .WrapAll((e, next) =>
                {
                    handlerCalls.Add("c");
                    next(e);
                })
                .WrapAll((e, next) =>
                {
                    handlerCalls.Add("b");
                    next(e);
                })
                .WrapAll((e, next) =>
                {
                    handlerCalls.Add("a");
                    next(e);
                });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();

            // assert
            handlerCalls.Should().BeInAscendingOrder();
        }

        [Test]
        public void When_multiple_consequenter_handlers_are_chained_then_the_last_added_is_called_first()
        {
            // arrange
            var bus = new InProcessEventBus();

            var handlerCalls = new List<string>();

            // act
            var handler = new TestConsequenter()
                .WrapAll((e, next) =>
                {
                    handlerCalls.Add("c");
                    next(e);
                })
                .WrapAll((e, next) =>
                {
                    handlerCalls.Add("b");
                    next(e);
                })
                .WrapAll((e, next) =>
                {
                    handlerCalls.Add("a");
                    next(e);
                });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();

            // assert
            handlerCalls.Should().BeInAscendingOrder();
        }

        [Test]
        public void Consequenter_can_be_short_circuited_using_handler_chains()
        {
            // arrange
            var bus = new InProcessEventBus();
            var consequenterWasCalled = false;
            var handlerCalls = 0;

            // act
            var handler = new TestConsequenter(onCreated: e => consequenterWasCalled = true)
                .WrapAll((e, next) =>
                {
                    handlerCalls++;
                    // by not calling next, we short circuit the call to the remaining handler chain
                });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();

            // assert
            handlerCalls.Should().Be(1);
            consequenterWasCalled.Should().BeFalse();
        }

        [Test]
        public void When_a_handler_chain_throws_then_an_EventHandlingError_is_published()
        {
            // arrange
            var bus = new InProcessEventBus();
            var errors = new List<EventHandlingError>();
            bus.Errors.Subscribe(errors.Add);

            // act
            var handler = new TestConsequenter()
                .WrapAll((e, next) => { throw new Exception("oops!"); });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();

            // assert
            errors.Should().ContainSingle(e => e.StreamName == "Order" &&
                                               e.Event.EventName() == "Created" &&
                                               e.Exception.Message.Contains("oops!"));
        }

        [Test]
        public async Task When_a_handler_chain_throws_then_subsequent_events_are_still_published()
        {
            // arrange
            var bus = new InProcessEventBus();
            var errors = new List<EventHandlingError>();
            bus.Errors.Subscribe(errors.Add);
            var callCount = 0;

            // act
            var handler = new TestConsequenter()
                .WrapAll((e, next) =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new Exception("oops!");
                    }
                });

            bus.Subscribe(handler);
            
            await bus.PublishAsync(new Order.Created());
            await bus.PublishAsync(new Order.Created());

            // assert
            callCount.Should().Be(2);
        }

        [Test]
        public void When_a_consequenter_that_has_been_chained_throws_then_the_EventHandlingError_Handler_references_the_inner_handler()
        {
            // arrange
            var bus = new InProcessEventBus();
            var errors = new List<EventHandlingError>();
            bus.Errors.Subscribe(errors.Add);

            // act
            var handler = new TestConsequenter(onCreated: e => { throw new Exception("oops!"); });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();

            // assert
            errors.Should().ContainSingle(e => e.Handler is TestConsequenter);
        }

        [Test]
        public void When_a_handler_chain_throws_then_the_EventHandlingError_Handler_references_the_inner_handler()
        {
            // arrange
            var bus = new InProcessEventBus();
            var errors = new List<EventHandlingError>();
            bus.Errors.Subscribe(errors.Add);

            // act
            var handler = new TestConsequenter()
                .WrapAll((e, next) => { throw new Exception("oops!"); });

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.Created()).Wait();

            // assert
            errors.Should().ContainSingle(e => e.Handler is TestConsequenter);
        }

        // TODO: (EventHandlingTests) test WrapAll with duck & dynamic handlers

        private class TestProjector :
            IUpdateProjectionWhen<Order.Cancelled>,
            IUpdateProjectionWhen<Order.Created>,
            IUpdateProjectionWhen<Order.Delivered>
        {
            private readonly Action<Order.Cancelled> onCancelled;
            private readonly Action<Order.Created> onCreated;
            private readonly Action<Order.Delivered> onDelivered;

            public TestProjector(
                Action<Order.Cancelled> onCancelled = null,
                Action<Order.Created> onCreated = null,
                Action<Order.Delivered> onDelivered = null)
            {
                this.onCancelled = onCancelled ?? (e => { });
                this.onCreated = onCreated ?? (e => { });
                this.onDelivered = onDelivered ?? (e => { });
            }

            public void UpdateProjection(Order.Cancelled @event)
            {
                onCancelled(@event);
            }

            public void UpdateProjection(Order.Created @event)
            {
                onCreated(@event);
            }

            public void UpdateProjection(Order.Delivered @event)
            {
                onDelivered(@event);
            }
        }
    }
}
