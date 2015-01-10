// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventHandlerTests
    {
        private FakeEventBus bus = new FakeEventBus();

        private static List<RecordedEvent> recordedEvents = new List<RecordedEvent>();

        private class RecordedEvent
        {
            public IEvent Event;
            public object Handler;
        }

        [SetUp]
        public void SetUp()
        {
            bus = new FakeEventBus();
            recordedEvents = new List<RecordedEvent>();
        }

        [Test]
        public void KnownHandlerTypes_includes_handlers_of_IEvent()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (UpdateProjectionWhenIEvent));
        }

        [Test]
        public void KnownHandlerTypes_includes_handlers_of_IEvent_of_T()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (UpdateProjectionWhenIEventOfOrder));
        }

        [Test]
        public void KnownHandlerTypes_includes_handlers_of_Event()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (UpdateProjectionWhenEvent));
        }

        [Test]
        public void KnownHandlerTypes_includes_handlers_of_Event_of_T()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (UpdateProjectionWhenEventOfOrder));
        }

        [Test]
        public void KnownHandlerTypes_includes_handlers_of_concrete_non_generic_event_types()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (UpdateProjectionWhenOrderPlaced));
        }

        [Test]
        public void KnownHandlerTypes_includes_handlers_implementing_more_than_one_IEvent_generic()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (UpdateProjectionWhenEventOfOrder));
        }

        [Test]
        public void KnownHandlerTypes_includes_derived_handlers_of_Event_of_T()
        {
            Event<Order>.KnownHandlerTypes.Should().Contain(typeof (DerivedIUpdateProjectionWhenEventOfOrder));
        }

        [Test]
        public void KnownHandlerTypes_does_not_include_abstract_handlers_of_Event_of_T()
        {
            Event<Order>.KnownHandlerTypes.Should().NotContain(typeof (AbstractIUpdateProjectionWhenEventOfOrder));
        }

        [Test]
        public void KnownHandlerTypes_does_not_include_handlers_of_Event_of_a_different_T()
        {
            Event<Order>.KnownHandlerTypes.Should().NotContain(typeof (UpdateProjectionWhenOfEventOfCustomerAccount));
        }

        [Test]
        public void Handlers_are_instantiated_and_subscribed_to_all_event_types_in_which_they_are_interested()
        {
            using (SubscribedHandlers())
            {
                bus.PublishAsync(
                    new Order.Cancelled(),
                    new CustomerAccount.EmailAddressChanged()).Wait();

                var handledEvents =
                    recordedEvents.Where(e => e.Handler is UpdateProjectionWhenEventsOrderAndOfCustomerAccount).ToArray();

                handledEvents.Count().Should().Be(2);
                var event1 = handledEvents.First().Event;
                var event2 = handledEvents.Skip(1).First().Event;

                event1.GetType().Should().NotBe(event2.GetType());
            }
        }

        [Test]
        public void No_handler_will_handle_the_same_event_more_than_once()
        {
            using (SubscribedHandlers())
            {
                bus.PublishAsync(new Order.Cancelled())
                   .Wait();

                var handlers = recordedEvents.Select(e => e.Handler).ToArray();
                handlers.Count().Should().BeGreaterThan(0);
                handlers.Count().Should().Be(handlers.Distinct().Count());
            }
        }

        [Test]
        public void When_Subscribe_is_called_with_a_handler_having_no_event_handler_implementations_then_it_throws()
        {
            var bus = new FakeEventBus();

            bus.Invoking(b => b.Subscribe(new object()))
               .ShouldThrow<ArgumentException>()
               .And
               .Message.Should().Contain("does not implement any event handler interfaces");
        }

        [Test]
        public void When_Subscribe_is_called_each_handler_interface_is_subscribed_once()
        {
            var bus = new FakeEventBus();

            bus.Subscribe(new Handler_of_5_more_event_types());

            bus.SubscribedEventTypes().Count().Should().Be(9);
        }

        [Test]
        public void When_Subscribe_is_called_then_redundant_implementations_are_not_subscribed_more_than_once()
        {
            var bus = new FakeEventBus();

            bus.Subscribe(new Handler_of_redundant_event_types());

            bus.SubscribedEventTypes().Count().Should().Be(9);
        }

        [Test]
        public void IProjectFrom_and_ITriggerProcessOn_interfaces_for_different_event_types_are_subscribed_on_the_same_handler_instance()
        {
            var bus = new FakeEventBus();

            bus.Subscribe(new Projector_and_Trigger_from_different_event_types());

            bus.SubscribedEventTypes().Count().Should().Be(2);
        }

        [Test]
        public void IProjectFrom_and_ITriggerProcessOn_interfaces_for_same_event_type_are_subscribed_on_the_same_handler_instance()
        {
            var bus = new FakeEventBus();

            bus.Subscribe(new Projector_and_Trigger_from_same_event_type());
            bus.PublishAsync(new Order.Placed()).Wait();

            bus.SubscribedEventTypes().Count().Should().Be(2);
            recordedEvents.Count().Should().Be(2);
        }

        [Test]
        public void When_an_exception_is_thrown_by_a_projector_then_it_continues_to_receive_events()
        {
            var count = 0;
            bus.Subscribe(Projector.Create<Order.Paid>(e =>
            {
                count++;
                if (count > 8)
                {
                    throw new Exception("oops!");
                }
            }));

            var events = Enumerable.Range(1, 10).Select(i => new Order.Paid(i)).ToArray();
            using (bus.PublishAsync(events).Subscribe())
            {
                count.Should().Be(10);
            }
        }

        [Test]
        public void When_an_exception_is_thrown_by_a_IHaveConsequencesWhen_then_it_continues_to_receive_events()
        {
            int count = 0;
            bus.Subscribe(Consequenter.Create<Order.Paid>(e =>
            {
                count++;
                if (count > 8)
                {
                    throw new Exception("oops!");
                }
            }));

            var events = Enumerable.Range(1, 10).Select(i => new Order.Paid(i)).ToArray();
            using (bus.PublishAsync(events).Subscribe())
            {
                count.Should().Be(10);
            }
        }

        [Test]
        public void EventHandler_Name_returns_handler_type_name_for_simple_consequenter()
        {
            EventHandler.Name(new HaveConsequencesWhenEventOfOrder())
                        .Should()
                        .Be("HaveConsequencesWhenEventOfOrder");
        }

        [Test]
        public void EventHandler_Name_returns_handler_type_name_for_simple_projector()
        {
            EventHandler.Name(new UpdateProjectionWhenEventOfOrder())
                        .Should()
                        .Be("UpdateProjectionWhenEventOfOrder");
        }

        [Test]
        public void EventHandler_Name_throws_for_non_event_handler()
        {
            Action checkName = () => EventHandler.Name(new object());

            checkName.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void EventHandler_Name_returns_Name_of_wrapped_handler()
        {
            var wrapper = new Handler_of_5_more_event_types().WrapAll((e, next) => { });

            EventHandler.Name(wrapper).Should().Be("Microsoft.Its.Domain.Tests.EventHandlerTests+Handler_of_5_more_event_types");
        }
        
        [Test]
        public void EventHandler_Name_returns_Name_of_derived_types()
        {
            var projector = new TypeDiscoveryTests.ConcreteProjector();

            EventHandler.Name(projector).Should().Be("ConcreteProjector");
        }

        [Test]
        public void EventHandler_Name_returns_the_type_name_for_EventHandlerBase_derived_projectors_where_the_Name_property_is_not_overridden()
        {
            var wrapper = new TypeDiscoveryTests.ConcreteEventHandler();

            EventHandler.Name(wrapper).Should().Be("ConcreteEventHandler");
        }

        [Test]
        public void EventHandler_Name_returns_something_vaguely_informative_for_anonymous_consequenters()
        {
            var wrapper = Consequenter.Create<Order.ItemAdded>(e => { });

            EventHandler.Name(wrapper).Should().Be("AnonymousConsequenter");
        }

        [Test]
        public void EventHandler_Name_returns_something_vaguely_informative_for_anonymous_projectors()
        {
            var wrapper = Projector.Create<Order.ItemAdded>(e => { });

            EventHandler.Name(wrapper).Should().Be("AnonymousProjector");
        }


        [Test]
        public void EventHandler_FullName_includes_namespaces()
        {
            var fullName = EventHandler.FullName(new Handler_of_4_event_types());

            fullName.Should().Be("Microsoft.Its.Domain.Tests.EventHandlerTests+Handler_of_4_event_types");
        }

        [Test]
        public void EventHandler_FullName_includes_generic_parameters_with_full_namespaces()
        {
            var fullName = EventHandler.FullName(new GenericProjector<IEvent>());

            fullName.Should().Be("Microsoft.Its.Domain.Tests.EventHandlerTests+GenericProjector(Microsoft.Its.Domain.IEvent)");
        }

        [Test]
        public void EventHandler_FullName_differentiates_anonymous_projectors_that_were_named_using_Named()
        {
            // deliberately different implementations to avoid compiler inlining -- basically the implementation is going to depend on the anonymous closure types being different
            var anonymousProjector1 = Projector.Create<Order.Cancelled>(e => Console.WriteLine(e.AggregateId)).Named("one");
            var anonymousProjector2 = Projector.Create<Order.Cancelled>(e => Console.WriteLine(e.SequenceNumber)).Named("two");

            var projector1Name = EventHandler.FullName(anonymousProjector1);
            var projector2Name = EventHandler.FullName(anonymousProjector2);

            projector1Name.Should().NotBe(projector2Name);
        }

        [Test]
        public void EventHandler_FullName_differentiates_anonymous_consequenters_that_were_named_using_Named()
        {
            // deliberately different implementations to avoid compiler inlining -- basically the implementation is going to depend on the anonymous closure types being different
            var anonymousConsequenter1 = Consequenter.Create<Order.Cancelled>(e => Console.WriteLine(e.AggregateId)).Named("one");
            var anonymousConsequenter2 = Consequenter.Create<Order.Cancelled>(e => Console.WriteLine(e.SequenceNumber)).Named("two");

            var consequenter1Name = EventHandler.FullName(anonymousConsequenter1);
            var consequenter2Name = EventHandler.FullName(anonymousConsequenter2);

            consequenter1Name.Should().NotBe(consequenter2Name);
        }

        [Test]
        public void EventHandler_FullName_differentiates_wrapped_anonymous_handlers_that_were_named_using_Named()
        {
            // deliberately different implementations to avoid compiler inlining -- basically the implementation is going to depend on the anonymous closure types being different
            var anonymousProjector1 = Projector.Create<Order.Cancelled>(e => Console.WriteLine(e.AggregateId))
                                               .WrapAll((e, next) => { })
                                               .Named("one");
            var anonymousProjector2 = Projector.Create<Order.Cancelled>(e => Console.WriteLine(e.SequenceNumber))
                                               .WrapAll((e, next) => { })
                                               .Named("two");

            var projector1Name = EventHandler.FullName(anonymousProjector1);
            var projector2Name = EventHandler.FullName(anonymousProjector2);

            projector1Name.Should().NotBe(projector2Name);
        }

        [Test]
        public void Named_does_not_prevent_calls_to_inner_handler()
        {
            var callCount = 0;
            var handler = Consequenter.Create<Order.CreditCardCharged>(e => callCount++)
                                      .Named("something");

            bus.Subscribe(handler);

            bus.PublishAsync(new Order.CreditCardCharged()).Wait();

            callCount.Should().Be(1);
        }

        [Test]
        public void A_named_assigned_using_Named_is_not_lost_on_successive_wraps()
        {
            var name = Any.CamelCaseName();
            var consequenter = Consequenter.Create<Order.CustomerInfoChanged>(e => { })
                                           .Named(name)
                                           .WrapAll((e, next) => next(e));

            EventHandler.Name(consequenter).Should().Be(name);
            EventHandler.FullName(consequenter).Should().Be(name);
        }

        [Test]
        public void InnerHandler_returns_correct_handler_for_singly_wrapped_handlers()
        {
            var handler = new Handler_of_5_more_event_types();

            var wrapped = handler.WrapAll((e, next) => { });

            wrapped.InnerHandler().Should().BeSameAs(handler);
        }

        [Test]
        public void InnerHandler_returns_correct_handler_for_multiply_wrapped_handlers()
        {
            var handler = new Handler_of_5_more_event_types();

            var wrapped = handler.WrapAll((e, next) => { })
                                 .WrapAll((e, next) => { })
                                 .WrapAll((e, next) => { });

            wrapped.InnerHandler().Should().BeSameAs(handler);
        }

        [Test]
        public void IsEventHandlerType_returns_true_for_consequenters()
        {
            Consequenter.Create<Order.ItemRemoved>(e => { })
                        .GetType()
                        .IsEventHandlerType()
                        .Should()
                        .BeTrue();
        }

        [Test]
        public void IsEventHandlerType_returns_true_for_projectors()
        {
            Projector.Create<Order.ItemRemoved>(e => { })
                     .GetType()
                     .IsEventHandlerType()
                     .Should()
                     .BeTrue();
        }

        private static void RecordHandling(IEvent e, object handler)
        {
            var recordedEvent = new RecordedEvent
            {
                Event = e,
                Handler = handler
            };
            recordedEvents.Add(recordedEvent);
        }

        private IDisposable SubscribedHandlers()
        {
            var handlers = Discover.EventHandlerTypes()
                                   .Where(t => t.IsNested && t.DeclaringType == GetType())
                                   .Select(Activator.CreateInstance)
                                   .ToArray();
            return bus.Subscribe(handlers);
        }

        #region event handler implementations

        public class UpdateProjectionWhenIEvent : IUpdateProjectionWhen<IEvent>
        {
            public void UpdateProjection(IEvent @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class UpdateProjectionWhenIEventOfOrder : IUpdateProjectionWhen<IEvent<Order>>
        {
            public void UpdateProjection(IEvent<Order> @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class UpdateProjectionWhenEvent : IUpdateProjectionWhen<Event>
        {
            public void UpdateProjection(Event @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class UpdateProjectionWhenEventOfOrder : IUpdateProjectionWhen<Event<Order>>
        {
            public void UpdateProjection(Event<Order> @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class HaveConsequencesWhenEventOfOrder : IHaveConsequencesWhen<Event<Order>>
        {
            public void HaveConsequences(Event<Order> @event)
            {
                RecordHandling(@event, this);
            }
        }

        public abstract class AbstractIUpdateProjectionWhenEventOfOrder : IUpdateProjectionWhen<IEvent<Order>>
        {
            public void UpdateProjection(IEvent<Order> @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class DerivedIUpdateProjectionWhenEventOfOrder : UpdateProjectionWhenEventOfOrder
        {
            public void Project(IEvent<Order> @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class UpdateProjectionWhenOfEventOfCustomerAccount : IUpdateProjectionWhen<Event<CustomerAccount>>
        {
            public void UpdateProjection(Event<CustomerAccount> @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class UpdateProjectionWhenEventsOrderAndOfCustomerAccount :
            IUpdateProjectionWhen<Order.Cancelled>,
            IUpdateProjectionWhen<CustomerAccount.EmailAddressChanged>
        {
            public void UpdateProjection(CustomerAccount.EmailAddressChanged @event)
            {
                RecordHandling(@event, this);
            }

            public void UpdateProjection(Order.Cancelled @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class UpdateProjectionWhenOrderPlaced :
            IUpdateProjectionWhen<Order.Placed>
        {
            public void UpdateProjection(Order.Placed @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class Handler_of_redundant_event_types :
            Handler_of_5_more_event_types,
            IUpdateProjectionWhen<Order.ItemAdded>,
            IUpdateProjectionWhen<CustomerAccount.EmailAddressChanged>
        {
        }

        public class Handler_of_5_more_event_types :
            Handler_of_4_event_types,
            IUpdateProjectionWhen<IEvent>,
            IUpdateProjectionWhen<IEvent<Order>>,
            IUpdateProjectionWhen<Order.ItemAdded>,
            IUpdateProjectionWhen<Order.ItemRemoved>,
            IUpdateProjectionWhen<Order.Delivered>
        {
            public void UpdateProjection(IEvent @event)
            {
            }

            public void UpdateProjection(IEvent<Order> @event)
            {
            }

            public void UpdateProjection(Order.ItemAdded @event)
            {
            }

            public new void UpdateProjection(CustomerAccount.EmailAddressChanged @event)
            {
            }

            public void UpdateProjection(Order.ItemRemoved @event)
            {
            }

            public void UpdateProjection(Order.Delivered @event)
            {
            }
        }

        public class Handler_of_4_event_types :
            IUpdateProjectionWhen<IEvent<CustomerAccount>>,
            IUpdateProjectionWhen<Order.ShippingMethodSelected>,
            IUpdateProjectionWhen<CustomerAccount.EmailAddressChanged>,
            IUpdateProjectionWhen<Order.Misdelivered>
        {
            public void UpdateProjection(IEvent<CustomerAccount> @event)
            {
            }

            public void UpdateProjection(Order.ShippingMethodSelected @event)
            {
            }

            public void UpdateProjection(CustomerAccount.EmailAddressChanged @event)
            {
            }

            public void UpdateProjection(Order.Misdelivered @event)
            {
            }
        }

        public class Projector_and_Trigger_from_same_event_type :
            IUpdateProjectionWhen<Order.Placed>,
            IHaveConsequencesWhen<Order.Placed>
        {
            public void UpdateProjection(Order.Placed @event)
            {
                RecordHandling(@event, this);
            }

            public void HaveConsequences(Order.Placed @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class Projector_and_Trigger_from_different_event_types :
            IUpdateProjectionWhen<CustomerAccount.EmailAddressChanged>,
            IHaveConsequencesWhen<Order.Placed>
        {
            public void UpdateProjection(CustomerAccount.EmailAddressChanged @event)
            {
                RecordHandling(@event, this);
            }

            public void HaveConsequences(Order.Placed @event)
            {
                RecordHandling(@event, this);
            }
        }

        public class GenericProjector<TEvent> : IUpdateProjectionWhen<TEvent>
            where TEvent : IEvent
        {
            public void UpdateProjection(TEvent @event)
            {
                RecordHandling(@event, this);
            }
        }

        #endregion
    }
}
