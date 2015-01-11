// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Its.Configuration;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Microsoft.Its.Domain.Testing;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class SerializerTests
    {
        [SetUp]
        public void SetUp()
        {
            Serializer.ConfigureDefault();
        }

        [Test]
        public void A_json_string_containing_multiple_events_can_be_deserialized_using_F()
        {
            var file = Settings.GetFile(f => f.Name == "Events.json");

            using (var stream = file.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var events = Serializer.FromJsonToEvents(json).ToArray();

                events.Count().Should().Be(8);
                events.Select(e => e.GetType()).Should().Contain(new[]
                {
                    typeof (Order.ItemAdded),
                    typeof (Order.ShippingMethodSelected),
                    typeof (Order.Placed),
                    typeof (Order.Paid),
                    typeof (Order.Shipped),
                    typeof (Order.Delivered),
                    typeof (Order.CreditCardCharged),
                    typeof (Order.Fulfilled)
                });
            }
        }

        [Test]
        public void Serializer_Settings_can_be_cloned()
        {
            var cloned = Serializer.CloneSettings();
            cloned.Converters.Count().Should().Be(Serializer.Settings.Converters.Count);
            cloned.ShouldBeEquivalentTo(Serializer.Settings);
        }

        [Test]
        public void Serializer_DeserializeEvent_finds_the_correct_event_type_when_EventNameAttribute_is_used()
        {
            var original = new EventTests.TestEventWithCustomName
            {
                AggregateId = Any.Guid(),
                SequenceNumber = Any.PositiveInt(),
                Value = Any.AlphanumericString(100)
            };

            var deserialized = Serializer.DeserializeEvent("Order", "Bob", original.AggregateId, original.SequenceNumber, original.Timestamp, original.ToJson());

            deserialized.ShouldBeEquivalentTo(original);
        }
    }
}
