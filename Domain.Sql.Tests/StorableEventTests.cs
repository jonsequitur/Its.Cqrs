// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsEventStore")]
    public class StorableEventTests : EventStoreDbTest
    {
        [Test]
        public void UtcTime_getter_accurately_converts_values_set_via_TimeStamp_setter()
        {
            var now = DateTime.UtcNow;

            var e = new StorableEvent
            {
                Timestamp = now
            };

            e.UtcTime.Should().Be(now);
        }

        [Test]
        public void UtcTime_setter_accurately_converts_values_forwarded_to_TimeStamp_setter()
        {
            var now = DateTime.UtcNow;

            var e = new StorableEvent
            {
                UtcTime = now
            };

            e.Timestamp.Should().Be(now);
        }

        [Test]
        public void Event_Actor_is_populated_from_Event_Metadata()
        {
            var actor = Any.Email();

            var sourceEvent = new Order.ItemAdded
            {
                Metadata =
                {
                    Actor = actor
                }
            };

            var storableEvent = sourceEvent.ToStorableEvent();

            storableEvent.Actor.Should().Be(actor);
        }

        [Test]
        public void When_Metadata_Actor_is_not_set_then_Event_Actor_will_be_null()
        {
            var sourceEvent = new Order.ItemAdded();

            var storableEvent = sourceEvent.ToStorableEvent();

            storableEvent.Actor.Should().BeNull();
        }

        [Test]
        public void Properties_on_the_base_Event_type_are_not_serialized()
        {
            var serialized = new TestEvent
            {
                AggregateId = Guid.NewGuid(),
                SequenceNumber = 1,
                Timestamp = DateTimeOffset.Now,
                Id = 7,
                Data = "abc"
            }.ToStorableEvent().Body;

            serialized.Should().Be("{\"Id\":7,\"Data\":\"abc\"}");
        }

        [Test]
        public void Timestamp_round_trips_correctly_between_Event_and_StorableEvent()
        {
            var e1 = new TestEvent
            {
                Timestamp = DateTimeOffset.Now,
            };
            var e2 = e1.ToStorableEvent().ToDomainEvent();

            e2.Timestamp.Should().Be(e1.Timestamp);
        }

        [Test]
        public void Timestamp_round_trips_correctly_to_database()
        {
            var id = Guid.NewGuid();
            var now = Clock.Now();

            using (var db = EventStoreDbContext())
            {
                db.Events.Add(new TestEvent
                {
                    AggregateId = id,
                    Timestamp = now
                }.ToStorableEvent());

                db.SaveChanges();
            }

            using (var db = EventStoreDbContext())
            {
                var @event = db.Events.Single(e => e.AggregateId == id).ToDomainEvent();

                // the database is not saving the offset, but the two dates should be equivalent UTC times 
                @event.Timestamp
                      .UtcDateTime
                      .Should()
                      .BeCloseTo(now.UtcDateTime,
                                 // there's a slight loss of precision saving to the db, but we should be within 3ms
                                 precision: 3
                    );
            }
        }

        public class TestEvent : Event<TestAggregate>
        {
            public int Id;
            public string Data;

            public override void Update(TestAggregate aggregate)
            {
            }
        }

        public class TestAggregate : EventSourcedAggregate<TestAggregate>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class.
            /// </summary>
            /// <param name="id">The aggregate's unique id.</param>
            /// <param name="eventHistory">The event history.</param>
            public TestAggregate(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="EventSourcedAggregate{T}"/> class.
            /// </summary>
            /// <param name="id">The aggregate's unique id.</param>
            public TestAggregate(Guid? id = null) : base(id)
            {
            }
        }
    }
}
