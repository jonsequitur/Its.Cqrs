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
using Test.Domain.Ordering;
using Test.Domain.Ordering;

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
        public void A_json_string_containing_multiple_events_can_be_deserialized_using_FromJsonToEvents()
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
            cloned.Converters.Count.Should().Be(Serializer.Settings.Converters.Count);
            cloned.ShouldBeEquivalentTo(Serializer.Settings, _ => _.ExcludingFields());
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

        [Test]
        public void Serializer_DeserializeEvent_can_deserialize_to_known_Event_T_types()
        {
            var aggregateId = Any.Guid();
            var sequenceNumber = Any.PositiveInt();
            var details = Any.Paragraph(10);
            var dateTimeOffset = Any.DateTimeOffset();
            var uniqueEventId = Any.Long();
            var eTag = Any.Word().ToETag();

            var deserialized = Serializer.DeserializeEvent(
                "Order",
                "Misdelivered",
                aggregateId,
                sequenceNumber,
                dateTimeOffset,
                new { Details = details }.ToJson(),
                uniqueEventId,
                etag: eTag);

            deserialized.Should().BeOfType<Order.Misdelivered>();

            var @event = (Order.Misdelivered) deserialized;

            @event.AggregateId.Should().Be(aggregateId);
            @event.SequenceNumber.Should().Be(sequenceNumber);
            @event.Details.Should().Be(details);
            @event.Timestamp.Should().Be(dateTimeOffset);
            @event.ETag.Should().Be(eTag);

            ((long) @event.Metadata.AbsoluteSequenceNumber).Should().Be(uniqueEventId);
        }

        [Test]
        public void Serializer_DeserializeEvent_can_deserialize_to_nested_IEvent_types()
        {
            var aggregateId = Any.Guid();
            var sequenceNumber = Any.PositiveInt();
            var value = Any.Paragraph(10);
            var dateTimeOffset = Any.DateTimeOffset();
            var eTag = Any.Word().ToETag();

            var deserialized = Serializer.DeserializeEvent(
                "DeserializationTestPoco",
                "DeserializationTest_IEvent",
                aggregateId,
                sequenceNumber,
                dateTimeOffset,
                new { Value = value }.ToJson(),
                etag: eTag);

            deserialized.Should().BeOfType<DeserializationTestPoco.DeserializationTest_IEvent>();

            var @event = (DeserializationTestPoco.DeserializationTest_IEvent) deserialized;

            @event.AggregateId.Should().Be(aggregateId);
            @event.SequenceNumber.Should().Be(sequenceNumber);
            @event.Value.Should().Be(value);
            @event.Timestamp.Should().Be(dateTimeOffset);
            @event.ETag.Should().Be(eTag);
        }
        
        [Test]
        public void Serializer_DeserializeEvent_can_deserialize_to_nested_Event_types()
        {
            var aggregateId = Any.Guid();
            var sequenceNumber = Any.PositiveInt();
            var value = Any.Paragraph(10);
            var dateTimeOffset = Any.DateTimeOffset();
            var eTag = Any.Word().ToETag();

            var deserialized = Serializer.DeserializeEvent(
                "DeserializationTestPoco",
                "DeserializationTest_Event",
                aggregateId,
                sequenceNumber,
                dateTimeOffset,
                new { Value = value }.ToJson(),
                etag: eTag);

            deserialized.Should().BeOfType<DeserializationTestPoco.DeserializationTest_Event>();

            var @event = (DeserializationTestPoco.DeserializationTest_Event) deserialized;

            @event.AggregateId.Should().Be(aggregateId);
            @event.SequenceNumber.Should().Be(sequenceNumber);
            @event.Value.Should().Be(value);
            @event.Timestamp.Should().Be(dateTimeOffset);
            @event.ETag.Should().Be(eTag);
        }
    }

    public class DeserializationTestPoco
    {
        public class DeserializationTest_IEvent : IEvent
        {
            public string Value { get; set; }
            public long SequenceNumber { get; set; }
            public Guid AggregateId { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string ETag { get; set; }
        }

        public class DeserializationTest_Event : Event
        {
            public string Value { get; set; }
        }
    }
}
