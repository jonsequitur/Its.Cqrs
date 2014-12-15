// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Its.Log.Instrumentation;
using Microsoft.Its.Recipes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Microsoft.Its.Domain;

namespace Microsoft.Its.EventStore.Tests
{
    public abstract class CommonEventStreamTests
    {
        private readonly string eventType;

        protected CommonEventStreamTests()
        {
            eventType = GetType().Name;
        }

        [Test]
        public void When_a_stream_is_appended_without_a_version_specified_then_it_increments()
        {
            var stream = GetEventStream();
            var id = Guid.NewGuid().ToString();
            var version = Any.PositiveInt();
            stream.Apply(eventType, "one", id, version: version);

            stream.Append(eventType, "two", id);

            var latest = stream.Latest(id);
            latest.SequenceNumber.Should().Be(version + 1);
        }

        [Test]
        public void Streams_can_be_appended_to_with_a_specified_version()
        {
            var stream = GetEventStream();

            string id = Guid.NewGuid().ToString();

            var version = Any.PositiveInt();
            stream.Append(eventType, "hello", id, version: version);

            stream.NextVersion(id).Should().Be(version + 1);
        }

        [Test]
        public void The_latest_value_can_be_retrieved_from_the_stream()
        {
            var stream = GetEventStream();
            string id = Guid.NewGuid().ToString();
            var version = Any.PositiveInt();
            string body = Any.String(20, 50, Characters.LatinLettersAndNumbers());
            
            stream.Append(eventType, "one", id, version: version);
            stream.Append(eventType, "two", id);
            stream.Append(eventType, body, id);

            var latest = stream.Latest(id);

            latest.Body.Should().Be(body);
        }

        [Test]
        public void NextVersion_returns_1_for_an_unknown_id()
        {
            var stream = GetEventStream();

            stream.NextVersion(Guid.NewGuid().ToString()).Should().Be(1);
        }

        [Test]
        public void NextVersion_returns_correct_value_for_existing_id()
        {
            var stream = GetEventStream();
            string id = Guid.NewGuid().ToString();

            stream.Append(eventType, "first", id);

            stream.NextVersion(id).Should().Be(2);
        }

        [Test]
        public void AsOfDate_returns_only_events_prior_to_the_specified_date()
        {
            var startTime = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(380));
            var queryingUpToTime = startTime.AddSeconds(4);
            var stream = GetEventStream();
            string id = Guid.NewGuid().ToString();

            for (var i = 1; i < 10; i++)
            {
                stream.Append(eventType, "event " + i, id, version: i, timestamp: startTime.AddSeconds(i));
            }

            Console.WriteLine(stream.All(id).ToLogString());
            Console.WriteLine(new { startTime, queryingUpToTime }.ToLogString());

            var events = stream.AsOfDate(id, queryingUpToTime);

            events.Count().Should().Be(4);
        }

        [Test]
        public void UpToVersion_returns_only_events_up_to_the_specified_version()
        {
            var stream = GetEventStream();
            string id = Guid.NewGuid().ToString();

            for (var i = 1; i < 10; i++)
            {
                stream.Append(eventType, "event " + i, id, version: i);
            }

            var events = stream.UpToVersion(id, 4);

            events.Count().Should().Be(4);
        }

        protected abstract IEventStream GetEventStream();
    }
}