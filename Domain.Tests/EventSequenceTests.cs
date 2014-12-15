// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventSequenceTests
    {
        [Test]
        public void When_events_are_added_having_undefined_aggregate_id_then_the_EventSequence_SourceId_is_assigned()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent());

            events.First().AggregateId.Should().Be(events.AggregateId);
        }

        [Test]
        public void When_events_are_added_having_defined_SequenceNumbers_then_the_ids_are_maintained()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent { SequenceNumber = 3 });
            events.Add(new TestEvent { SequenceNumber = 2 });
            events.Add(new TestEvent { SequenceNumber = 1 });

            events.First().SequenceNumber.Should().Be(events.Last().SequenceNumber - 2);
        }

        [Test]
        public void When_events_are_added_without_defined_SequenceNumbers_then_they_are_assigned_the_next_sequential_value()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent { SequenceNumber = 2 });
            events.Add(new TestEvent());
            events.Add(new TestEvent());

            events.Last().SequenceNumber.Should().Be(4);
        }

        [Test]
        public void When_events_are_added_with_with_gaps_in_their_SequenceNumbers_then_they_are_assigned_the_next_sequential_value()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent { SequenceNumber = 22 });
            events.Add(new TestEvent { SequenceNumber = 33 });
            events.Add(new TestEvent());

            events.Last().SequenceNumber.Should().Be(34);
        }

        [Test]
        public void Events_are_ordered_by_SequenceNumber_regardless_of_the_order_in_which_they_are_added()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent { SequenceNumber = 3 });
            events.Add(new TestEvent { SequenceNumber = 2 });
            events.Add(new TestEvent { SequenceNumber = 1 });
            events.Add(new TestEvent { SequenceNumber = 4 });

            events.Select(e => e.SequenceNumber).Should().BeInAscendingOrder();
        }

        [Test]
        public void Events_added_out_of_order_do_not_have_their_SequenceNumbers_changed()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent { SequenceNumber = 3, Data = "3" });
            events.Add(new TestEvent { SequenceNumber = 2, Data = "2" });
            events.Add(new TestEvent { SequenceNumber = 1, Data = "1" });
            events.Add(new TestEvent { SequenceNumber = 4, Data = "4" });

            events.Cast<TestEvent>().Select(e => e.Data)
                  .SequenceEqual(new[] { "1", "2", "3", "4" });
        }

        [Test]
        public void When_an_event_with_an_already_present_SequenceNumber_is_added_then_it_throws()
        {
            var events = new EventSequence(Guid.NewGuid());

            events.Add(new TestEvent { SequenceNumber = 1 });

            Action addAgain = () =>
                              events.Add(new TestEvent { SequenceNumber = 1 });

            addAgain.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void When_an_event_with_a_non_matching_aggregate_id_is_added_then_it_throws()
        {
            var list = new EventSequence(Guid.NewGuid());

            list.Invoking(l => l.Add(new TestEvent { AggregateId = Guid.NewGuid() }))
                .ShouldThrow<ArgumentException>();
        }

        [Test]
        public void EventSequence_version_starts_at_zero()
        {
            var sequence = new EventSequence(Guid.NewGuid());

            sequence.Version.Should().Be(0);
        }
        
        [Test]
        public void EventSequence_version_returns_current_highest_SequenceNumber()
        {
            var sequence = new EventSequence(Guid.NewGuid());

            sequence.Add(new TestEvent());
            sequence.Add(new TestEvent());
            sequence.Add(new TestEvent());

            sequence.Version.Should().Be(3);
        }

        [Test]
        public void EventSequence_version_returns_current_highest_SequenceNumber_when_events_are_non_contiguous()
        {
            var sequence = new EventSequence(Guid.NewGuid());

            sequence.Add(new TestEvent{SequenceNumber = 4});
            sequence.Add(new TestEvent{SequenceNumber = 9});
            sequence.Add(new TestEvent{SequenceNumber = 9000});

            sequence.Version.Should().Be(9000);
        }

        [Test]
        public void EventSequence_version_returns_current_highest_SequenceNumber_when_events_are_out_of_order()
        {
            var sequence = new EventSequence(Guid.NewGuid());

            sequence.Add(new TestEvent { SequenceNumber = 4 });
            sequence.Add(new TestEvent { SequenceNumber = 9000 });
            sequence.Add(new TestEvent { SequenceNumber = 9 });

            sequence.Version.Should().Be(9000);
        }

        public class TestEvent : Event
        {
            public string Data;
        }
    }
}