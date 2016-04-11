// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides lookup and persistence for event sourced aggregates.
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public interface IEventSourcedRepository<TAggregate> : IStore<TAggregate> 
        where TAggregate : class, IEventSourced
    {
        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        Task<TAggregate> GetLatest(Guid aggregateId);

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        Task<TAggregate> GetVersion(Guid aggregateId, long version);

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        Task<TAggregate> GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate);

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        Task Save(TAggregate aggregate);
    }
}
