using System;
using System.Linq;
using FluentAssertions;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class QueryableExtensionsTests : EventStoreDbTest
    {
        [Test]
        public void RelatedEvents_returns_all_events_in_related_aggregates()
        {
            // arrange
            var relatedId1 = Any.Guid();
            var relatedId2 = Any.Guid();
            var relatedId3 = Any.Guid();
            var relatedId4 = Any.Guid();
            var unrelatedId = Any.Guid();

            Console.WriteLine(new
            {
                relatedId1,
                relatedId2,
                relatedId3,
                relatedId4,
                unrelatedId
            }.ToLogString());

            using (var db = new EventStoreDbContext())
            {
                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId1,
                    SequenceNumber = i,
                    Body = new { relatedId2 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "one",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId2,
                    SequenceNumber = i,
                    Body = new { relatedId3 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "two",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId3,
                    SequenceNumber = i,
                    Body = new { relatedId4 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId4,
                    SequenceNumber = i,
                    Body = new { }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event" + i.ToString()
                }));

                Enumerable.Range(1, 20).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = unrelatedId,
                    SequenceNumber = i,
                    Body = new { }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event" + i.ToString()
                }));

                db.SaveChanges();
            }

            using (var db = new EventStoreDbContext())
            {
                //act
                var events = db.Events.RelatedEvents(relatedId1).ToArray();

                // assert
                events.Count().Should().Be(80);
                events.Should().Contain(e => e.AggregateId == relatedId1);
                events.Should().Contain(e => e.AggregateId == relatedId2);
                events.Should().Contain(e => e.AggregateId == relatedId3);
                events.Should().Contain(e => e.AggregateId == relatedId4);
                events.Should().NotContain(e => e.AggregateId == unrelatedId);
            }
        }

        [Test]
        public void RelatedEvents_handles_circular_references()
        {
            var relatedId1 = Any.Guid();
            var relatedId2 = Any.Guid();
            var relatedId3 = Any.Guid();
            Console.WriteLine(new
            {
                relatedId1,
                relatedId2,
                relatedId3,
            }.ToLogString());

            using (var db = new EventStoreDbContext())
            {
                db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId1,
                    SequenceNumber = 1,
                    Body = new { relatedId2 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "one",
                    Type = "Event"
                });

                db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId2,
                    SequenceNumber = 1,
                    Body = new { relatedId3 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "two",
                    Type = "Event"
                });

                db.Events.Add(new StorableEvent
                {
                    AggregateId = relatedId3,
                    SequenceNumber = 1,
                    Body = new { relatedId1 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "three",
                    Type = "Event"
                });

                db.SaveChanges();
            }

            // assert
            using (var db = new EventStoreDbContext())
            {
                var events = db.Events.RelatedEvents(relatedId1).ToArray();

                events.Count().Should().Be(3);
                events.Should().Contain(e => e.AggregateId == relatedId1);
                events.Should().Contain(e => e.AggregateId == relatedId2);
                events.Should().Contain(e => e.AggregateId == relatedId3);
            }
        }

        [Test]
        public void RelatedEvents_can_return_several_nonintersecting_graphs_at_once()
        {
            var graph1Id1 = Any.Guid();
            var graph1Id2 = Any.Guid();
            var graph2Id1 = Any.Guid();
            var graph2Id2 = Any.Guid();

            Console.WriteLine(new
            {
                graph1Id1,
                graph1Id2,
                graph2Id1,
                graph2Id2
            }.ToLogString());

            using (var db = new EventStoreDbContext())
            {
                db.Events.Add(new StorableEvent
                {
                    AggregateId = graph1Id1,
                    SequenceNumber = 1,
                    Body = new { graph1Id2 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "one",
                    Type = "Event"
                });

                db.Events.Add(new StorableEvent
                {
                    AggregateId = graph1Id2,
                    SequenceNumber = 1,
                    Body = new { }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "one",
                    Type = "Event"
                });

                db.Events.Add(new StorableEvent
                {
                    AggregateId = graph2Id1,
                    SequenceNumber = 1,
                    Body = new { graph2Id2 }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "two",
                    Type = "Event"
                });
                db.Events.Add(new StorableEvent
                {
                    AggregateId = graph2Id2,
                    SequenceNumber = 1,
                    Body = new { }.ToJson(),
                    Timestamp = Clock.Now(),
                    StreamName = "two",
                    Type = "Event"
                });

                db.SaveChanges();
            }

            // assert
            using (var db = new EventStoreDbContext())
            {
                var events = db.Events.RelatedEvents(graph1Id1, graph2Id1).ToArray();

                events.Count().Should().Be(4);
                events.Should().Contain(e => e.AggregateId == graph1Id1);
                events.Should().Contain(e => e.AggregateId == graph1Id2);
                events.Should().Contain(e => e.AggregateId == graph2Id1);
                events.Should().Contain(e => e.AggregateId == graph2Id2);
            }
        }
    }
}