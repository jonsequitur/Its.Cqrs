// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.EventHandling;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Projections;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Catchups")]
    [TestFixture]
    public class ReportingProjectorTests : EventStoreDbTest
    {
        [SetUp]
        public override void SetUp()
        {
            Console.WriteLine("ReportingProjectorTests.SetUp");

            base.SetUp();

            // reset order tallies
            using (var db = new ReadModelDbContext())
            {
                var tallies = db.Set<OrderTally>();
                tallies.ToArray().ForEach(t => tallies.Remove(t));
                db.SaveChanges();
            }
        }

        [Test]
        public void CreateDynamic_queries_all_events()
        {
            Events.Write(10);
            var projectedEventCount = 0;

            using (var catchup = CreateReadModelCatchup(Projector.CreateDynamic(e =>
            {
                projectedEventCount++;
            })))
            {
                catchup.Run();
            }

            projectedEventCount.Should().Be(10);
        }

        [Test]
        public void A_dynamic_projector_can_access_the_properties_of_known_event_types_dynamically()
        {
            var expectedName = Any.FullName();
            string actualName = null;
            Events.Write(1, _ => new Order.CustomerInfoChanged
            {
                CustomerName = expectedName
            });

            using (var catchup = CreateReadModelCatchup(Projector.CreateDynamic(e =>
            {
                actualName = e.CustomerName;
            })))
            {
                catchup.Run();
            }

            actualName.Should().Be(expectedName);
        }

        [Test]
        public void A_dynamic_projector_receives_known_event_types_as_their_actual_type()
        {
            IEvent receivedEvent = null;
            Events.Write(1, _ => new Order.CustomerInfoChanged());

            using (var catchup = CreateReadModelCatchup(Projector.CreateDynamic(e =>
            {
                receivedEvent = e;
            })))
            {
                catchup.Run();
            }

            receivedEvent.Should().BeOfType<Order.CustomerInfoChanged>();
        }

        [Test]
        public void A_dynamic_projector_can_access_the_properties_of_unknown_event_types_dynamically()
        {
            using (var db = new EventStoreDbContext())
            {
                Enumerable.Range(1, 10).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = Guid.NewGuid(),
                    StreamName = Any.CamelCaseName(),
                    Type = Any.CamelCaseName(),
                    Body = new { SomeValue = i }.ToJson()
                }));
                db.SaveChanges();
            }
            var total = 0;

            using (var catchup = CreateReadModelCatchup(Projector.CreateDynamic(e =>
            {
                total += (int) e.SomeValue;
            })))
            {
                catchup.Run();
            }

            total.Should().Be(55);
        }

        [Test]
        public void A_dynamic_projector_can_specify_which_events_to_query()
        {
            var eventsQueried = 0;
            var expectedEventsQueried = 0;
            Events.Write(20, _ =>
            {
                Event e = null;
                switch (Any.Int(1, 3))
                {
                    case 1:
                        e = (Event) Events.Any();
                        break;
                    case 2:
                        e = new Order.ItemAdded();
                        break;
                    case 3:
                        e = new Order.ItemRemoved();
                        break;
                }

                if (e is Order.ItemAdded || e is Order.ItemRemoved)
                {
                    expectedEventsQueried++;
                }

                e.AggregateId = Guid.NewGuid();

                return e;
            });

            using (var catchup = CreateReadModelCatchup(Projector.CreateDynamic(e =>
            {
                eventsQueried++;
            },
                                                                                "Order.ItemAdded",
                                                                                "Order.ItemRemoved")))
            {
                catchup.Run();
            }

            eventsQueried.Should().Be(expectedEventsQueried);
        }

        [Test]
        public void When_classes_not_implementing_IEvent_are_used_to_query_the_event_store_then_only_the_corresponding_events_are_queried()
        {
            var eventsQueried = 0;
            var expectedEventsQueried = 0;
            Events.Write(20, _ =>
            {
                Event e = null;
                switch (Any.Int(1, 3))
                {
                    case 1:
                        e = (Event) Events.Any();
                        break;
                    case 2:
                        e = new Order.ItemAdded();
                        break;
                    case 3:
                        e = new Order.ItemRemoved();
                        break;
                }

                if (e is Order.ItemAdded || e is Order.ItemRemoved)
                {
                    expectedEventsQueried++;
                }

                e.AggregateId = Guid.NewGuid();

                return e;
            });

            var projector1 = Projector.CreateFor<ItemAdded>(e =>
            {
                eventsQueried++;
            });
            var projector2 = Projector.CreateFor<ItemRemoved>(e =>
            {
                eventsQueried++;
            });

            using (var catchup = CreateReadModelCatchup(projector1, projector2))
            {
                catchup.Run();
            }

            eventsQueried.Should().Be(expectedEventsQueried);
        }

        [Test]
        public void When_classes_not_implementing_IEvent_are_used_to_query_the_event_store_then_properties_are_duck_deserialized()
        {
            using (var db = new EventStoreDbContext())
            {
                Enumerable.Range(1, 5).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = Guid.NewGuid(),
                    StreamName = "Reporting",
                    Type = "DuckEvent",
                    Body = new { Quacks = i }.ToJson()
                }));

                Enumerable.Range(1, 5).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = Guid.NewGuid(),
                    StreamName = Any.CamelCaseName(),
                    Type = "DuckEvent",
                    Body = new { Quacks = i }.ToJson()
                }));

                db.SaveChanges();
            }
            var unNestedDuckQuacks = 0;
            var nestedDuckQuacks = 0;

            var projector1 = Projector.CreateFor<DuckEvent>(e =>
            {
                unNestedDuckQuacks += e.Quacks;
            });
            var projector2 = Projector.CreateFor<Reporting.DuckEvent>(e =>
            {
                nestedDuckQuacks += e.Quacks;
            });

            using (var catchup = CreateReadModelCatchup(projector1, projector2))
            {
                catchup.Run();
            }

            nestedDuckQuacks.Should().Be(15);
            unNestedDuckQuacks.Should().Be(30);
        }

        [Test]
        public void When_classes_not_implementing_IEvent_are_used_to_query_the_event_store_then_nested_classes_can_be_used_to_specify_stream_names()
        {
            using (var db = new EventStoreDbContext())
            {
                Enumerable.Range(1, 5).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = Guid.NewGuid(),
                    StreamName = "Reporting",
                    Type = "DuckEvent",
                    Body = new { Quacks = i }.ToJson()
                }));

                Enumerable.Range(1, 5).ForEach(i => db.Events.Add(new StorableEvent
                {
                    AggregateId = Guid.NewGuid(),
                    StreamName = Any.CamelCaseName(),
                    Type = "DuckEvent",
                    Body = new { Quacks = i }.ToJson()
                }));

                db.SaveChanges();
            }
            var unNestedDucks = 0;
            var nestedDucks = 0;

            var projector1 = Projector.CreateFor<DuckEvent>(e =>
            {
                unNestedDucks++;
            });
            var projector2 = Projector.CreateFor<Reporting.DuckEvent>(e =>
            {
                nestedDucks++;
            });

            using (var catchup = CreateReadModelCatchup(projector1, projector2))
            {
                catchup.Run();
            }

            nestedDucks.Should().Be(5);
            unNestedDucks.Should().Be(10);
        }

        [Test]
        public void When_classes_not_implementing_IEvent_are_used_to_query_the_event_store_then_non_nested_classes_have_their_EventStreamName_set_correctly()
        {
            Events.Write(1, _ => new Order.Cancelled());
            Cancelled cancelled = null;
            var projector = Projector.CreateFor<Cancelled>(e =>
            {
                cancelled = e;
            });

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            cancelled.EventStreamName.Should().Be("Order");
        }

        [Test]
        public void Dynamic_projectors_can_access_the_event_properties_of_received_events()
        {
            var expectedTimestamp = DateTimeOffset.Parse("2014-07-03");
            var expectedAggregateId = Any.Guid();
            var receivedAggregateId = new Guid();
            var expectedSequenceNumber = Any.PositiveInt();
            long receivedSequenceNumber = 0;
            var receivedTimestamp = new DateTimeOffset();
            var receivedEventStreamName = "";
            var receivedEventTypeName = "";

            using (var db = new EventStoreDbContext())
            {
                db.Events.Add(new StorableEvent
                {
                    AggregateId = expectedAggregateId,
                    SequenceNumber = expectedSequenceNumber,
                    Timestamp = expectedTimestamp,
                    StreamName = "Reporting",
                    Type = "DuckEvent",
                    Body = new { Quacks = 9000 }.ToJson()
                });

                db.SaveChanges();
            }

            var projector = Projector.CreateDynamic(e =>
            {
                receivedAggregateId = e.AggregateId;
                receivedSequenceNumber = e.SequenceNumber;
                receivedTimestamp = e.Timestamp;
                receivedEventStreamName = e.EventStreamName;
                receivedEventTypeName = e.EventTypeName;
            });

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            receivedAggregateId.Should().Be(expectedAggregateId);
            receivedSequenceNumber.Should().Be(expectedSequenceNumber);
            receivedTimestamp.Should().Be(expectedTimestamp);
            receivedEventStreamName.Should().Be("Reporting");
            receivedEventTypeName.Should().Be("DuckEvent");
        }

        [Test]
        public void Duck_projectors_can_access_the_event_properties_of_received_events()
        {
            var expectedTimestamp = DateTimeOffset.Parse("2014-07-03");
            var expectedAggregateId = Any.Guid();
            var receivedAggregateId = new Guid();
            var expectedSequenceNumber = Any.PositiveInt();
            int receivedSequenceNumber = 0;
            var receivedTimestamp = new DateTimeOffset();
            var receivedEventStreamName = "";
            var receivedEventTypeName = "";

            using (var db = new EventStoreDbContext())
            {
                db.Events.Add(new StorableEvent
                {
                    AggregateId = expectedAggregateId,
                    SequenceNumber = expectedSequenceNumber,
                    Timestamp = expectedTimestamp,
                    StreamName = "Reporting",
                    Type = "DuckEvent",
                    Body = new { Quacks = 9000 }.ToJson()
                });

                db.SaveChanges();
            }

            var projector = Projector.CreateFor<Reporting.DuckEvent>(e =>
            {
                receivedAggregateId = e.AggregateId;
                receivedSequenceNumber = e.SequenceNumber;
                receivedTimestamp = e.Timestamp;
                receivedEventStreamName = e.EventStreamName;
                receivedEventTypeName = e.EventTypeName;
            });

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            receivedAggregateId.Should().Be(expectedAggregateId);
            receivedSequenceNumber.Should().Be(expectedSequenceNumber);
            receivedTimestamp.Should().Be(expectedTimestamp);
            receivedEventStreamName.Should().Be("Reporting");
            receivedEventTypeName.Should().Be("DuckEvent");
        }

        [Test]
        public void Report_projections_can_be_created_using_anonymous_projectors_over_multiple_event_types()
        {
            var projector = Projector.Combine(
                Projector.CreateFor<Placed>(e =>
                {
                    using (var db1 = new ReadModelDbContext())
                    {
                        db1.OrderTallyByStatus(OrderTally.OrderStatus.Pending).Count ++;
                        db1.SaveChanges();
                    }
                }),
                Projector.CreateFor<Cancelled>(e =>
                {
                    using (var db2 = new ReadModelDbContext())
                    {
                        db2.OrderTallyByStatus(OrderTally.OrderStatus.Canceled).Count ++;
                        db2.OrderTallyByStatus(OrderTally.OrderStatus.Pending).Count --;
                        db2.SaveChanges();
                    }
                }),
                Projector.CreateFor<Delivered>(e =>
                {
                    using (var db3 = new ReadModelDbContext())
                    {
                        db3.OrderTallyByStatus(OrderTally.OrderStatus.Delivered).Count ++;
                        db3.OrderTallyByStatus(OrderTally.OrderStatus.Pending).Count --;
                        db3.SaveChanges();
                    }
                })).Named("Order status report");

            Events.Write(20, _ => new Order.Cancelled());
            Events.Write(20, _ => new Order.Delivered());
            Events.Write(50, _ => new Order.Placed());

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            using (var db = new ReadModelDbContext())
            {
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Canceled")
                  .Count
                  .Should()
                  .Be(20);
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Delivered")
                  .Count
                  .Should()
                  .Be(20);
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Pending")
                  .Count
                  .Should()
                  .Be(10);
            }
        }

        [Test]
        public void Report_projections_can_be_created_using_EventHandlerBase_over_multiple_event_types()
        {
            var projector = new OrderTallyProjector();

            Events.Write(20, _ => new Order.Cancelled());
            Events.Write(20, _ => new Order.Delivered());
            Events.Write(50, _ => new Order.Placed());

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            using (var db = new ReadModelDbContext())
            {
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Canceled")
                  .Count
                  .Should()
                  .Be(20);
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Delivered")
                  .Count
                  .Should()
                  .Be(20);
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Pending")
                  .Count
                  .Should()
                  .Be(10);
            }
        }

        [Test]
        public void Report_projections_can_be_created_using_EventHandlerBase_including_dynamic_event_types()
        {
            var projector = new OrderTallyProjector();

            Events.Write(1, _ => new Order.Cancelled());
            Events.Write(20, _ => new Order.Fulfilled());
            Events.Write(4, _ => new Order.Misdelivered());

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            using (var db = new ReadModelDbContext())
            {
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Fulfilled")
                  .Count
                  .Should()
                  .Be(20);
                db.Set<OrderTally>()
                  .Single(t => t.Status == "Misdelivered")
                  .Count
                  .Should()
                  .Be(4);
            }
        }

        [Ignore("This would require some sort of proxy to access properties that are not on Event")]
        [Test]
        public void All_events_can_be_subscribed_using_EventHandlerBase_On_Event()
        {
            var projector = new EventTallyProjector<Event>();

            Events.Write(20, _ => new Order.Cancelled());
            Events.Write(20, _ => new Order.Delivered());
            Events.Write(50, _ => new Order.Placed());

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            projector.EventCount.Should().Be(90);
        }

        [Test]
        public void All_events_can_be_subscribed_using_EventHandlerBase_On_IEvent()
        {
            var projector = new EventTallyProjector<IEvent>();

            Events.Write(20, e => new Order.Cancelled());
            Events.Write(20, e => new Order.Delivered());
            Events.Write(50, e => new Order.Placed());

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            projector.EventCount.Should().Be(90);
        }

        [Test]
        public void All_events_can_be_subscribed_using_EventHandlerBase_On_object()
        {
            var projector = new EventTallyProjector<object>();

            Events.Write(20, _ => new Order.Cancelled());
            Events.Write(20, _ => new Order.Delivered());
            Events.Write(50, _ => new Order.Placed());

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            projector.EventCount.Should().Be(90);
        }

        [Test]
        public async Task Catchup_continues_when_DuckTypeProjector_throws()
        {
            Events.Write(20);
            int counter = 0;
            int eventsHandled = 0;
            var handler = Projector.CreateFor<ItemAdded>(e =>
            {
                counter++;
                if (counter == 10)
                {
                    throw new Exception("oops!");
                }
                eventsHandled++;
            });

            await new ReadModelCatchup(handler)
            {
                StartAtEventId = HighestEventId + 1
            }.SingleBatchAsync();

            eventsHandled.Should().Be(19);
        }

        [Test]
        public async Task Catchup_continues_when_DynamicProjector_throws()
        {
            Events.Write(20);
            int counter = 0;
            int eventsHandled = 0;
            var handler = Projector.CreateDynamic(e =>
            {
                counter++;
                if (counter == 10)
                {
                    throw new Exception("oops!");
                }
                eventsHandled++;
            });

            await new ReadModelCatchup(handler)
            {
                StartAtEventId = HighestEventId + 1
            }.SingleBatchAsync();

            eventsHandled.Should().Be(19);
        }
    }

    public class ItemAdded
    {
        public Guid AggregateId { get; set; }
        public long SequenceNumber { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }

    public class ItemRemoved
    {
        public Guid AggregateId { get; set; }
        public long SequenceNumber { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }

    public class Cancelled
    {
        public string EventStreamName { get; set; }
    }

    public class Placed
    {
    }

    public class Delivered : IEvent
    {
        public string EventStreamName { get; set; }
        public long SequenceNumber { get; private set; }
        public Guid AggregateId { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string ETag { get; set; }
    }

    public class DuckEvent
    {
        public int Quacks { get; set; }
    }

    public class Reporting
    {
        public class DuckEvent
        {
            public int Quacks { get; set; }
            public Guid AggregateId { get; set; }
            public int SequenceNumber { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string EventStreamName { get; set; }
            public string EventTypeName { get; set; }
        }

        public class SimpleEvent : Event
        {
        }
    }

    public static class DbSetExtensions
    {
        public static OrderTally OrderTallyByStatus(
            this ReadModelDbContext db,
            OrderTally.OrderStatus status)
        {
            var statusString = status.ToString();
            var dbSet = db.Set<OrderTally>();
            return dbSet
                .SingleOrDefault(o => o.Status == statusString)
                .IfNotNull()
                .Else(() =>
                {
                    var tally = new OrderTally
                    {
                        Status = statusString
                    };

                    dbSet.Add(tally);

                    return tally;
                });
        }
    }

    public class OrderTallyProjector : EventHandlerBase
    {
        public OrderTallyProjector()
        {
            On<Placed>(e =>
            {
                using (var db = new ReadModelDbContext())
                {
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Pending).Count ++;
                    db.SaveChanges();
                }
            });

            On<Cancelled>(e =>
            {
                using (var db = new ReadModelDbContext())
                {
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Canceled).Count ++;
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Pending).Count --;
                    db.SaveChanges();
                }
            });

            On<Delivered>(e =>
            {
                using (var db = new ReadModelDbContext())
                {
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Delivered).Count ++;
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Pending).Count --;
                    db.SaveChanges();
                }
            });

            On("Order.Fulfilled", e =>
            {
                using (var db = new ReadModelDbContext())
                {
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Fulfilled).Count ++;
                    db.SaveChanges();
                }
            });

            On("Order.Misdelivered", e =>
            {
                using (var db = new ReadModelDbContext())
                {
                    db.OrderTallyByStatus(OrderTally.OrderStatus.Misdelivered).Count ++;
                    db.SaveChanges();
                }
            });
        }

        public override string Name
        {
            get
            {
                return "Order tally by status";
            }
        }
    }

    public class EventTallyProjector<T> : EventHandlerBase
    {
        public EventTallyProjector()
        {
            On<T>(e =>
            {
                EventCount++;
            });
        }

        public int EventCount { get; set; }
    }
}