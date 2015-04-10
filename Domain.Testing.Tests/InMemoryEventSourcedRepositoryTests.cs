// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class InMemoryEventSourcedRepositoryTests : EventSourcedRepositoryTests
    {
        private InMemoryEventStream eventStream;

        [SetUp]
        public override void SetUp()
        {
            eventStream = new InMemoryEventStream(AggregateType<Order>.EventStreamName);
            base.SetUp();
        }
        
        protected override void Configure(Configuration configuration, Action onSave = null)
        {
            configuration.UseEventBus(new FakeEventBus())
                         .UseDependency<IEventStream>(_ => eventStream)
                         .UseInMemoryEventStore()
                         .IgnoreScheduledCommands();
        }

        protected override IEventSourcedRepository<TAggregate> CreateRepository<TAggregate>(
            Action onSave = null)
        {
            if (onSave != null)
            {
                eventStream.BeforeSave += (sender, @event) => onSave();
            }

            return Configuration.Current.Repository<TAggregate>();
        }

        [Test]
        public override void When_storage_fails_then_no_events_are_published()
        {
            var repository = CreateRepository<Order>(onSave: () => { throw new ConcurrencyException("oops!"); });

            var order = new Order();
            Action save = () =>
                          repository.Save(order);

            save.ShouldThrow<ConcurrencyException>();

            (Configuration.Current.EventBus as FakeEventBus)
                .PublishedEvents()
                .Count()
                .Should()
                .Be(0);
        }

        [Test]
        public override void Events_at_the_end_of_the_sequence_that_cannot_be_deserialized_due_to_unknown_type_do_not_cause_Version_to_be_incorrect()
        {
            var orderId = Guid.NewGuid();
            var events = new List<IStoredEvent>
            {
                new Order.CustomerInfoChanged
                {
                    CustomerName = "Waylon Jennings",
                    AggregateId = orderId
                }.ToStoredEvent(),
                new InMemoryStoredEvent
                {
                    Type = "UKNOWN",
                    Body = new { ShoeSize = 10.5 }.ToJson(),
                    SequenceNumber = 2,
                    AggregateId = orderId.ToString(),
                    Timestamp = DateTime.UtcNow
                }
            };

            SaveEventsDirectly(events.ToArray());

            var repository = CreateRepository<Order>();

            var order = repository.GetLatest(orderId);

            order.Version.Should().Be(2);
        }

        [Test]
        public override void Events_that_cannot_be_deserialized_due_to_unknown_type_do_not_cause_sourcing_to_fail()
        {
            var orderId = Guid.NewGuid();
            var events = new List<IStoredEvent>
            {
                new InMemoryStoredEvent
                {
                    Type = "UKNOWN",
                    Body = new { ShoeSize = 10.5 }.ToJson(),
                    SequenceNumber = 1,
                    AggregateId = orderId.ToString(),
                    Timestamp = DateTimeOffset.UtcNow
                },
                new Order.CustomerInfoChanged
                {
                    CustomerName = "Waylon Jennings",
                    AggregateId = orderId,
                    SequenceNumber = 2
                }.ToStoredEvent()
            };

            SaveEventsDirectly(events.ToArray());

            var repository = CreateRepository<Order>();

            var order = repository.GetLatest(orderId);

            order.CustomerName.Should().Be("Waylon Jennings");
        }

        [Test]
        public override void Events_that_cannot_be_deserialized_due_to_unknown_member_do_not_cause_sourcing_to_fail()
        {
            var orderId = Guid.NewGuid();
            var goodEvent = new Order.CustomerInfoChanged
            {
                CustomerName = "Waylon Jennings",
                AggregateId = orderId,
                SequenceNumber = 1
            }.ToStoredEvent();
            var badEvent = new InMemoryStoredEvent
            {
                Type = goodEvent.Type,
                AggregateId = orderId.ToString(),
                SequenceNumber = 2,
                Body = new
                {
                    CustomerName = "Willie Nelson",
                    HairColor = "red"
                }.ToJson(),
                Timestamp = DateTimeOffset.UtcNow
            };

            SaveEventsDirectly(goodEvent, badEvent);

            var repository = CreateRepository<Order>();

            var order = repository.GetLatest(orderId);

            order.CustomerName.Should().Be("Willie Nelson");
        }

        protected override void SaveEventsDirectly(params object[] events)
        {
            eventStream.Append(
                events
                    .Select(e => e.IfTypeIs<IStoredEvent>()
                                  .Else(() => e.IfTypeIs<IEvent>()
                                               .Then(ee => ee.ToStoredEvent()))
                                  .ElseDefault())
                    .ToArray())
                       .Wait();
        }
    }
}
