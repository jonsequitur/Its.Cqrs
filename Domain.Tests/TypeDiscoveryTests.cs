// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Test.Domain.Ordering;
#pragma warning disable 618

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class TypeDiscoveryTests
    {
        [Test]
        public void Discover_EventHandlerTypes_discovers_both_projectors_and_consequences()
        {
            var types = Discover.EventHandlerTypes().ToArray();

            types.Should().Contain(typeof (ConcreteProjector));
            types.Should().Contain(typeof (ConcreteConsequenter));
        }

        [Test]
        public void Discover_Projectors_finds_projector_types()
        {
            var types = Discover.ProjectorTypes();

            types.Should().Contain(typeof (ConcreteProjector));
        }

        [Test]
        public void Discover_Projectors_does_not_include_abstract_types()
        {
            var types = Discover.ProjectorTypes();

            types.Should().NotContain(typeof (AbstractProjector));
        }

        [Test]
        public void Discover_Projectors_does_not_include_consequenters()
        {
            var types = Discover.ProjectorTypes();

            types.Should().NotContain(typeof (ConcreteConsequenter));
        }

        [Test]
        public void Discover_Consequenters_finds_consequenter_types()
        {
            var types = Discover.Consequenters();

            types.Should().Contain(typeof (ConcreteConsequenter));
        }

        [Test]
        public void Discover_Consequenters_does_not_include_abstract_types()
        {
            var types = Discover.Consequenters();

            types.Should().NotContain(typeof (AbstractConsequenter));
        }

        [Test]
        public void Discover_Consequenters_does_not_include_projectors()
        {
            var types = Discover.Consequenters();

            types.Should().NotContain(typeof (ConcreteProjector));
        }

        [Test]
        public void Discover_EventHandlerTypes_returns_a_given_type_only_once()
        {
            var types = Discover.EventHandlerTypes().ToArray();

            types.Should().ContainSingle(t => t == typeof (ConcreteConsequenter));
            types.Should().ContainSingle(t => t == typeof (ConcreteProjector));
        }

        [Test]
        public void EventHandlerBase_derived_classes_are_discoverable_as_event_handlers()
        {
            var types = Discover.EventHandlerTypes().ToArray();

            types.Should().ContainSingle(t => t == typeof (ConcreteProjector));
        }
        
        [Test]
        public void EventHandlerBase_derived_classes_are_discoverable_as_projectors()
        {
            var types = Discover.ProjectorTypes().ToArray();

            types.Should().ContainSingle(t => t == typeof (ConcreteProjector));
        }

        public abstract class AbstractConsequenter : IHaveConsequencesWhen<IEvent<Order>>
        {
            public abstract void HaveConsequences(IEvent<Order> @event);
        }

        public class ConcreteConsequenter : AbstractConsequenter
        {
            public override void HaveConsequences(IEvent<Order> @event)
            {
            }
        }

        public abstract class AbstractProjector :
            IUpdateProjectionWhen<IEvent<Order>>,
            IUpdateProjectionWhen<Order.Cancelled>
        {
            public abstract void UpdateProjection(IEvent<Order> @event);

            public void UpdateProjection(Order.Cancelled @event)
            {
            }
        }

        public class ConcreteProjector : AbstractProjector
        {
            public override void UpdateProjection(IEvent<Order> @event)
            {
            }
        }

        public class ConcreteEventHandler : EventHandlerBase
        {
        }
    }
}
