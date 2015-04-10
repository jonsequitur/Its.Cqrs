// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
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
        Task Append(Domain.Testing.IStoredEvent[] events);

        /// <summary>
        /// Gets the latest event in the stream with the specified id.
        /// </summary>
        /// <returns></returns>
        Task<Domain.Testing.IStoredEvent> Latest(string id);

        /// <summary>
        /// Gets all of the events in the stream having the specified id.
        /// </summary>
        Task<IEnumerable<Domain.Testing.IStoredEvent>> All(string id);

        /// <summary>
        /// Gets all of the events in the stream created having the specified id as of the specified date.
        /// </summary>
        Task<IEnumerable<Domain.Testing.IStoredEvent>> AsOfDate(string id, DateTimeOffset date);

        /// <summary>
        /// Gets all of the events in the stream created having the specified id up to (and including) the specified version.
        /// </summary>
        Task<IEnumerable<Domain.Testing.IStoredEvent>> UpToVersion(string id, long version);
    }
}
