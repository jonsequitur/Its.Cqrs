// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class EventExtensionsTests
    {
        [Test]
        public void ToStorableEvent_returns_nested_class_name_for_non_domain_event_classes()
        {
            var e = new Reporting.SimpleEvent();

            e.ToStorableEvent().StreamName.Should().Be("Reporting");
        }

        [Test]
        public void ToStorableEvent_uses_EventStreamName_property_if_present_on_non_domain_event_classes()
        {
            var e = new Delivered
            {
                EventStreamName = Any.Word()
            };

            e.ToStorableEvent().StreamName.Should().Be(e.EventStreamName);
        }

        [Test]
        public void ToStorableEvent_throws_when_event_is_null()
        {
            Event e = null;

            Action toStorableEvent = () => e.ToStorableEvent();

            toStorableEvent.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void ToStorableEvent_does_not_throw_when_actor_is_null()
        {
            Event e = new Annotated<CustomerAccount>("hi");

            e.SetActor((string) null);

            Action toStorableEvent = () => e.ToStorableEvent();

            toStorableEvent.ShouldNotThrow();
        }

        [Test]
        public void ToStorableEvent_restores_Id_from_metadata()
        {
            Event e = new Annotated<CustomerAccount>("hi");

            e.Metadata.AbsoluteSequenceNumber = 123;

            e.ToStorableEvent().Id.Should().Be(123);
        }
    }
}
