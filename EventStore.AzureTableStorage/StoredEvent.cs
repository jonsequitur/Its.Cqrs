// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Its.EventStore.AzureTableStorage
{
    public class StoredEvent : TableEntity, IStoredEvent
    {
        private dynamic metadata;

        // TODO: (StorableEvent) figure out appropriate ETag usage
        public StoredEvent()
        {
            Timestamp = DateTimeOffset.UtcNow;
            ClientTimestamp = DateTimeOffset.UtcNow;
        }

        public long SequenceNumber
        {
            get
            {
                return RowKey.FromRowKeyToSequenceNumber();
            }
            set
            {
                RowKey = value.ToRowKey();
            }
        }

        public string AggregateId
        {
            get
            {
                return PartitionKey;
            }
            set
            {
                PartitionKey = value;
            }
        }

        public dynamic Metadata
        {
            get
            {
                return metadata ?? (metadata = new ExpandoObject());
            }
        }

        public string Type { get; set; }

        public string Body { get; set; }

        // TODO: (StorableEvent.ClientTimestamp) is this actually needed? 
        public DateTimeOffset ClientTimestamp { get; set; }
    }
}