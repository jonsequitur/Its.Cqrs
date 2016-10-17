// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [UseSqlEventStore]
    [UseInMemoryCommandScheduling]
    public class SqlEventSourcedRepositoryTests : EventSourcedRepositoryTests
    {
        protected override IEventSourcedRepository<TAggregate> CreateRepository<TAggregate>(
            Action onSave = null)
        {
            var repository = Configuration.Current.Repository<TAggregate>() as SqlEventSourcedRepository<TAggregate>;

            if (onSave != null)
            {
                Console.WriteLine("onSave");
//                repository.GetEventStoreContext = () =>
//                {
//                    onSave();
//                    return EventStoreDbContext();
//                };
            }

            return repository;
        }

        protected override async Task SaveEventsDirectly(params InMemoryStoredEvent[] events)
        {
            var storableEvents =
                events.Select(
                    e =>
                    new StorableEvent
                    {
                        AggregateId = Guid.Parse(e.AggregateId),
                        Body = e.Body,
                        ETag = e.ETag,
                        SequenceNumber = e.SequenceNumber,
                        StreamName = e.StreamName,
                        Timestamp = e.Timestamp,
                        Type = e.Type,
                        UtcTime = e.Timestamp.UtcDateTime
                    });

            using (var db = EventStoreDbContext())
            {
                foreach (var @event in storableEvents)
                {
                    db.Events.Add(@event);
                }
                await db.SaveChangesAsync();
            }
        }

        protected override async Task DeleteEventsFromEventStore(Guid aggregateId)
        {
            using (var db = EventStoreDbContext())
            {
                db.Database.ExecuteSqlCommand($"DELETE FROM EventStore.Events WHERE AggregateId = '{aggregateId}'");
                await db.SaveChangesAsync();
            }
        }

        protected override InMemoryStoredEvent CreateStoredEvent(
            string streamName, 
            string type, 
            Guid aggregateId, 
            int sequenceNumber, 
            string body, 
            DateTime utcTime) =>
            new StorableEvent
            {
                StreamName = streamName,
                Type = type,
                AggregateId = aggregateId,
                SequenceNumber = sequenceNumber,
                Body = body,
                UtcTime = utcTime
            }.ToInMemoryStoredEvent();

        [Test]
        public override async Task Events_that_cannot_be_deserialized_due_to_unknown_type_do_not_cause_sourcing_to_fail()
        {
            Guid orderId = Guid.NewGuid();
            var events = new List<StorableEvent>
            {
                new StorableEvent
                {
                    StreamName = "Order",
                    Type = "UKNOWN",
                    Body = new { ShoeSize = 10.5 }.ToJson(),
                    SequenceNumber = 1,
                    AggregateId = orderId,
                    UtcTime = DateTime.UtcNow
                },
                new Order.CustomerInfoChanged
                {
                    CustomerName = "Waylon Jennings",
                    AggregateId = orderId,
                    SequenceNumber = 2
                }.ToStorableEvent()
            };

            using (var db = EventStoreDbContext())
            {
                db.Events.AddOrUpdate(events.ToArray());
                db.SaveChanges();
            }

            var repository = CreateRepository<Order>();

            var order = await repository.GetLatest(orderId);

            order.CustomerName.Should().Be("Waylon Jennings");
        }

        [Test]
        public override async Task Events_at_the_end_of_the_sequence_that_cannot_be_deserialized_due_to_unknown_type_do_not_cause_Version_to_be_incorrect()
        {
            var orderId = Guid.NewGuid();
            var events = new List<StorableEvent>
            {
                new Order.CustomerInfoChanged
                {
                    CustomerName = "Waylon Jennings",
                    AggregateId = orderId
                }.ToStorableEvent(),
                new StorableEvent
                {
                    StreamName = "Order",
                    Type = "UKNOWN",
                    Body = new { ShoeSize = 10.5 }.ToJson(),
                    SequenceNumber = 2,
                    AggregateId = orderId,
                    UtcTime = DateTime.UtcNow
                }
            };

            using (var db = EventStoreDbContext())
            {
                db.Events.AddOrUpdate(events.ToArray());
                db.SaveChanges();
            }

            var repository = CreateRepository<Order>();

            var order = await repository.GetLatest(orderId);

            order.Version.Should().Be(2);
        }

        [Test]
        public override async Task Events_that_cannot_be_deserialized_due_to_unknown_member_do_not_cause_sourcing_to_fail()
        {
            var orderId = Guid.NewGuid();
            var goodEvent = new Order.CustomerInfoChanged
            {
                CustomerName = "Waylon Jennings",
                AggregateId = orderId,
                SequenceNumber = 1
            }.ToInMemoryStoredEvent();
            var badEvent = new StorableEvent
            {
                StreamName = goodEvent.StreamName,
                Type = goodEvent.Type,
                AggregateId = Guid.Parse(goodEvent.AggregateId),
                SequenceNumber = 2,
                Body = new
                {
                    CustomerName = "Willie Nelson",
                    HairColor = "red"
                }.ToJson(),
                UtcTime = DateTime.UtcNow
            }.ToInMemoryStoredEvent();

            await SaveEventsDirectly(goodEvent, badEvent);

            var repository = CreateRepository<Order>();

            var order = await repository.GetLatest(orderId);

            order.CustomerName.Should().Be("Willie Nelson");
        }

        [Test]
        public override async Task When_storage_fails_then_no_events_are_published()
        {
            var order = new Order();
            var bus = new InProcessEventBus();
            var eventsPublished = new List<IEvent>();
            bus.Events<IEvent>().Subscribe(eventsPublished.Add);
            Func<EventStoreDbContext> eventStoreDbContext = () =>
            {
                throw new Exception("oops!");
            };
            var repository =new SqlEventSourcedRepository<Order>(bus, eventStoreDbContext);

            order
                .Apply(new AddItem
                {
                    ProductName = "Widget",
                    Price = 10m,
                    Quantity = 2
                });

            try
            {
                await repository.Save(order);
            }
            catch
            {
            }

            eventsPublished.Should().BeEmpty();
        }

        [Test]
        public async Task Repository_will_not_try_to_source_events_from_a_different_aggregate_type()
        {
            var id = Guid.NewGuid();
            var e = new StorableEvent
            {
                AggregateId = id,
                Type = "SomeEventType",
                StreamName = "SomeAggregateType",
                Body = new { Something = Any.Words() }.ToJson()
            }.ToInMemoryStoredEvent();

            await SaveEventsDirectly(e);

            var repository = CreateRepository<Order>();

            var order = await repository.GetLatest(id);

            order.Should().BeNull();
        }
    }
}
