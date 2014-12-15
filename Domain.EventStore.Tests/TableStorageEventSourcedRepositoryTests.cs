// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Its.Configuration;
using Microsoft.Its.Domain.EventStore.AzureTableStorage;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.EventStore.AzureTableStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.EventStore.Tests
{
    [TestFixture, TestClass]
    public class TableStorageEventSourcedRepositoryTests : EventSourcedRepositoryTests
    {
        private readonly CloudTable table;
        private string connectionString;

        public TableStorageEventSourcedRepositoryTests()
        {
            Settings.Sources = new ISettingsSource[]{ new ConfigDirectorySettings(@"c:\dev\.config" ) }.Concat(Settings.Sources);

            connectionString = Settings.Get<TableStorageSettings>().ConnectionString;
            table = CloudStorageAccount.Parse(connectionString)
                                       .CreateCloudTableClient()
                                       .GetTableReference(AggregateType<Order>.EventStreamName);
            table.CreateIfNotExists();
        }

        protected override IEventSourcedRepository<Order> CreateRepository(IEventBus bus = null, Action onSave = null)
        {
            var repository = new TableStorageEventSourcedRepository<Order>(CloudStorageAccount.Parse(connectionString), bus);

            if (onSave != null)
            {
                repository.GetCloudTable = () =>
                {
                    onSave();
                    return table;
                };
            }

            return repository;
        }

        protected override void SaveEventsDirectly(params object[] storedEvents)
        {
            var batch = new TableBatchOperation();

            foreach (var storedEvent in storedEvents)
            {
                batch.Insert((ITableEntity) storedEvent);
            }

            table.ExecuteBatch(batch);
        }

        [Test]
        public override void Events_that_cannot_be_deserialized_due_to_unknown_type_do_not_cause_sourcing_to_fail()
        {
            var orderId = Guid.NewGuid();
            var events = new List<StoredEvent>
            {
                new StoredEvent
                {
                    Type = "UKNOWN",
                    Body = new { ShoeSize = 10.5 }.ToJson(),
                    SequenceNumber = 1,
                    AggregateId = orderId.ToString(),
                    ClientTimestamp = DateTimeOffset.UtcNow
                },
                new Order.CustomerInfoChanged
                {
                    CustomerName = "Waylon Jennings",
                    AggregateId = orderId,
                    SequenceNumber = 2
                }.ToStoredEvent()
            };

            SaveEventsDirectly(events.ToArray());

            var repository = CreateRepository();

            var order = repository.GetLatest(orderId);

            order.CustomerName.Should().Be("Waylon Jennings");
        }

        [Test]
        public override void Events_at_the_end_of_the_sequence_that_cannot_be_deserialized_due_to_unknown_type_do_not_cause_Version_to_be_incorrect()
        {
            var orderId = Guid.NewGuid();
            var events = new List<StoredEvent>
            {
                new Order.CustomerInfoChanged
                {
                    CustomerName = "Waylon Jennings",
                    AggregateId = orderId
                }.ToStoredEvent(),
                new StoredEvent
                {
                    Type = "UKNOWN",
                    Body = new { ShoeSize = 10.5 }.ToJson(),
                    SequenceNumber = 2,
                    AggregateId = orderId.ToString(),
                    ClientTimestamp = DateTime.UtcNow
                }
            };

            SaveEventsDirectly(events.ToArray());

            var repository = CreateRepository();

            var order = repository.GetLatest(orderId);

            order.Version.Should().Be(2);
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
            var badEvent = new StoredEvent
            {
                Type = goodEvent.Type,
                AggregateId = orderId.ToString(),
                SequenceNumber = 2,
                Body = new
                {
                    CustomerName = "Willie Nelson",
                    HairColor = "red"
                }.ToJson(),
                ClientTimestamp = DateTimeOffset.UtcNow,
            };

            SaveEventsDirectly(goodEvent, badEvent);

            var repository = CreateRepository();

            var order = repository.GetLatest(orderId);

            order.CustomerName.Should().Be("Willie Nelson");
        }

        [Test]
        public override void When_storage_fails_then_no_events_are_published()
        {
            var order = new Order();
            var bus = new FakeEventBus();
            bus.Events<IEvent>().Subscribe(e => { throw new Exception("oops"); });
            var repository = CreateRepository(bus, () => { throw new Exception("oops!"); });

            order
                .Apply(new AddItem
                {
                    ProductName = "Widget",
                    Price = 10m,
                    Quantity = 2
                });

            try
            {
                repository.Save(order);
            }
            catch
            {
            }

            var query = new TableQuery<StoredEvent>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, order.Id.ToString())
                );
            var results = table.ExecuteQuery(query);
            results.Count().Should().Be(0);
        }
    }
}