// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.EventStore.AzureTableStorage;

namespace Microsoft.Its.Domain.EventStore.AzureTableStorage
{
    public static class EventExtensions
    {
        /// <summary>
        /// Creates a <see cref="StoredEvent" /> based on the specified domain event.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate to which the domain event applies.</typeparam>
        /// <param name="domainEvent">The domain event.</param>
        public static StoredEvent ToStoredEvent<TAggregate>(this IEvent<TAggregate> domainEvent)
            where TAggregate : IEventSourced
        {
            return new StoredEvent
            {
                RowKey = domainEvent.SequenceNumber.ToRowKey(),
                PartitionKey = domainEvent.AggregateId.ToString(),
                Timestamp = domainEvent.Timestamp,
                ClientTimestamp = domainEvent.Timestamp,
                Type = domainEvent.EventName(),
                Body = domainEvent.ToJson()
            };
        }
    }
}