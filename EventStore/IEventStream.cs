// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Its.EventStore
{
    /// <summary>
    /// Represents an append-only stream of events.
    /// </summary>
    public interface IEventStream
    {
        /// <summary>
        /// Appends a events to the stream.
        /// </summary>
        /// <param name="events">The events to append to the stream.</param>
        Task Append(IStoredEvent[] events);

        /// <summary>
        /// Gets the latest event in the stream with the specified id.
        /// </summary>
        /// <returns></returns>
        Task<IStoredEvent> Latest(string id);

        /// <summary>
        /// Gets all of the events in the stream having the specified id.
        /// </summary>
        Task<IEnumerable<IStoredEvent>> All(string id);

        /// <summary>
        /// Gets all of the events in the stream created having the specified id as of the specified date.
        /// </summary>
        Task<IEnumerable<IStoredEvent>> AsOfDate(string id, DateTimeOffset date);

        /// <summary>
        /// Gets all of the events in the stream created having the specified id up to (and including) the specified version.
        /// </summary>
        Task<IEnumerable<IStoredEvent>> UpToVersion(string id, long version);
    }
}