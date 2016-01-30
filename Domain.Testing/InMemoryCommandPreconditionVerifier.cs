// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    public class InMemoryCommandPreconditionVerifier : ICommandPreconditionVerifier
    {
        private readonly InMemoryEventStream eventStream;

        public InMemoryCommandPreconditionVerifier(InMemoryEventStream eventStream)
        {
            if (eventStream == null)
            {
                throw new ArgumentNullException("eventStream");
            }
            this.eventStream = eventStream;
        }

        public async Task<bool> HasBeenApplied(string scope, string etag)
        {
            return eventStream.Events.Any(e => e.AggregateId.ToString() == scope &&
                                               e.ETag == etag);
        }
    }
}