// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Its.EventStore.AzureTableStorage
{
    public class EventStream : IEventStream
    {
        internal readonly Func<DateTimeOffset> now;
        private readonly Lazy<CloudTable> table;

        public EventStream(
            string streamName,
            CloudStorageAccount storageAccount,
            Func<DateTimeOffset> now = null)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentException("streamName");
            }

            if (storageAccount == null)
            {
                throw new ArgumentNullException("storageAccount");
            }

            this.now = now ?? (() => DateTimeOffset.UtcNow);

            table = new Lazy<CloudTable>(() =>
            {
                var tableClient = storageAccount.CreateCloudTableClient();
                var t = tableClient.GetTableReference(streamName);
                t.CreateIfNotExists();
                return t;
            });
        }

        public void Append(IStoredEvent e)
        {
            var storableEvent = e.ToStoredEvent();
            var op = TableOperation.Insert(storableEvent);
            table.Value.Execute(op);
        }

        public IStoredEvent Latest(string aggregateId)
        {
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, aggregateId);

            var query = new TableQuery<StoredEvent>()
                .Take(1)
                .Where(filter);

            return table.Value
                        .ExecuteQuery(query)
                        .Take(1)
                        .ToArray()
                        .SingleOrDefault();
        }

        public IEnumerable<IStoredEvent> All(string aggregateId)
        {
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, aggregateId);

            var query = new TableQuery<StoredEvent>()
                .Where(filter);

            return table.Value
                        .ExecuteQuery(query)
                        .ToArray();
        }

        public IEnumerable<IStoredEvent> AsOfDate(string aggregateId, DateTimeOffset date)
        {
            var aggregateIdFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, aggregateId);
            var dateFilter = TableQuery.GenerateFilterConditionForDate("ClientTimestamp", QueryComparisons.LessThanOrEqual, date);
            var combined = TableQuery.CombineFilters(aggregateIdFilter, TableOperators.And, dateFilter);

            var query = new TableQuery<StoredEvent>()
                .Where(combined);

            return table.Value
                        .ExecuteQuery(query)
                        .ToArray();
        }

        public IEnumerable<IStoredEvent> UpToVersion(string aggregateId, long version)
        {
            var aggregateIdFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, aggregateId);
            var dateFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, version.ToRowKey());
            var combined = TableQuery.CombineFilters(aggregateIdFilter, TableOperators.And, dateFilter);

            var query = new TableQuery<StoredEvent>()
                .Where(combined);

            return table.Value
                        .ExecuteQuery(query)
                        .ToArray();
        }
    }
}