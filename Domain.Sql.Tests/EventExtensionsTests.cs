// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;

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
    }
}
