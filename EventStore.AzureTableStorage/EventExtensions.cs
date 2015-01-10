// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.EventStore.AzureTableStorage
{
    public static class EventExtensions
    {
        public static StoredEvent ToStoredEvent(this IStoredEvent e)
        {
            return new StoredEvent
            {
                SequenceNumber = e.SequenceNumber,
                AggregateId = e.AggregateId,
                ClientTimestamp = e.Timestamp,
                Type = e.Type,
                Body = e.Body
            };
        }
    }
}
