using System;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class DynamicEventTests
    {
        [Test]
        public void When_DynamicEvent_is_instantiated_via_a_JObject_then_AggregateId_can_be_retrieved_via_IEvent_interface()
        {
            var guid = Any.Guid();
            var json = new
            {
                AggregateId = guid
            }.ToJson();

            IEvent dynamicEvent = new DynamicEvent(
                json.FromJsonTo<dynamic>());

            dynamicEvent.AggregateId.Should().Be(guid);
        }

        [Test]
        public void When_AggregateId_is_set_then_it_can_be_retrieved()
        {
            var e = new DynamicEvent(new JObject());

            var guid = Any.Guid();
            e.AggregateId = guid;

            e.AggregateId.Should().Be(guid);
        }

        [Test]
        public void When_DynamicEvent_is_instantiated_via_a_JObject_then_Timestamp_can_be_retrieved_via_IEvent_interface()
        {
            var timestamp = Any.DateTimeOffset();
            var json = new
            {
                Timestamp = timestamp
            }.ToJson();

            var jobject = json.FromJsonTo<dynamic>();

            IEvent dynamicEvent = new DynamicEvent(jobject);

            dynamicEvent.Timestamp.Should().Be(timestamp);
        }

        [Test]
        public void When_Timestamp_is_set_then_it_can_be_retrieved()
        {
            var e = new DynamicEvent(new JObject());

            var timestamp = Any.DateTimeOffset();
            e.Timestamp = timestamp;

            e.Timestamp.Should().Be(timestamp);
        }
    }
}