// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Verifies whether an etag has been recorded.
    /// </summary>
    public class InMemoryEventStoreETagChecker : IETagChecker
    {
        private readonly InMemoryEventStream eventStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventStoreETagChecker"/> class.
        /// </summary>
        /// <param name="eventStream">The event stream.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public InMemoryEventStoreETagChecker(InMemoryEventStream eventStream)
        {
            if (eventStream == null)
            {
                throw new ArgumentNullException(nameof(eventStream));
            }
            this.eventStream = eventStream;
        }

        /// <summary>
        /// Determines whether the specified etag has been recorded within the specified scope.
        /// </summary>
        /// <param name="scope">The scope within which the etag is unique.</param>
        /// <param name="etag">The etag.</param>
        public Task<bool> HasBeenRecorded(string scope, string etag) =>
            Task.FromResult(eventStream.Events
                                       .Any(e => e.AggregateId.ToString() == scope &&
                                                 e.ETag == etag));
    }
}