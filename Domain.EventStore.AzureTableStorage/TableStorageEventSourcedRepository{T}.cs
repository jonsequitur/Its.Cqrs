// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.EventStore;
using Microsoft.Its.EventStore.AzureTableStorage;
using Microsoft.Its.Recipes;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Its.Domain.EventStore.AzureTableStorage
{
    public class TableStorageEventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly IEventBus bus;
        private readonly CloudTableClient tableClient;
        private readonly CloudTable table;
        private IEventStream eventStream;

        public TableStorageEventSourcedRepository(
            CloudStorageAccount storageAccount,
            IEventBus bus = null)
        {
            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }

            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(AggregateType<TAggregate>.EventStreamName);
            table.CreateIfNotExists();
            GetCloudTable = () => table;
            this.bus = bus ?? InProcessEventBus.Instance;
            eventStream = new EventStream(AggregateType<TAggregate>.EventStreamName, storageAccount);
        }

        private TAggregate Get(Guid id, long? version = null)
        {
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id.ToString());
            if (version != null)
            {
                filter = TableQuery.CombineFilters(
                    filter,
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(
                        "RowKey",
                        QueryComparisons.GreaterThan,
                        (version.Value + 1).ToRowKey()));
            } 

            var query = new TableQuery<StoredEvent>().Where(filter);

            var events = table.ExecuteQuery(query).ToArray();

            if (events.Any())
            {
                return EventStore.EventExtensions.CreateAggregate<TAggregate>(id, events);
            }

            return null;
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetLatest(Guid aggregateId)
        {
            return Get(aggregateId);
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetVersion(Guid aggregateId, long version)
        {
            return Get(aggregateId, version);
        }

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        public TAggregate GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
        {
            var events = eventStream.AsOfDate(aggregateId.ToString(), asOfDate);
            return EventStore.EventExtensions.CreateAggregate<TAggregate>(aggregateId, events);
        }

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        public void Save(TAggregate aggregate)
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException("aggregate");
            }

            var events = aggregate.PendingEvents
                                  .Do(e => e.SetAggregate(aggregate))
                                  .ToArray();

            var storableEvents = events.OfType<IEvent<TAggregate>>().Select(e =>
            {
                var storableEvent = e.ToStoredEvent();
                return storableEvent;
            }).ToArray();

            var batch = new TableBatchOperation();

            foreach (var storableEvent in storableEvents)
            {
                batch.Insert(storableEvent);
            }

            // TODO: (Save) some retries
            try
            {
                GetCloudTable().ExecuteBatch(batch);
            }
            catch (StorageException exception)
            {
                if (exception.ToString().Contains("The specified entity already exists"))
                {
                    throw new ConcurrencyException("There was a concurrency violation.", events, exception);
                }
                throw;
            }

            storableEvents.ForEach(storableEvent =>
                                   events.Single(e => e.SequenceNumber == storableEvent.SequenceNumber)
                                         .IfTypeIs<IHaveExtensibleMetada>()
                                         .ThenDo(
                                             e => e.Metadata.AbsoluteSequenceNumber = storableEvent.Timestamp.Ticks));

            // move pending events to the event history
            aggregate.IfTypeIs<EventSourcedAggregate>()
                     .ThenDo(a => a.ConfirmSave());

            // publish the events
            bus.PublishAsync(events)
               .Subscribe(
                   onNext: e => { },
                   onError: ex =>
                   {
                       // TODO: (Save) logging 
                   });
        }

        public Func<CloudTable> GetCloudTable;
    }
}