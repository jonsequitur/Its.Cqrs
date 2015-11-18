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

        public async Task<bool> IsPreconditionSatisfied(IScheduledCommand scheduledCommand)
        {
            if (scheduledCommand == null)
            {
                throw new ArgumentNullException("scheduledCommand");
            }

            if (scheduledCommand.DeliveryPrecondition == null)
            {
                return true;
            }

            var aggregateId = scheduledCommand.DeliveryPrecondition.AggregateId.ToString();
            var etag = scheduledCommand.DeliveryPrecondition.ETag;

            return eventStream.Events.Any(a => a.AggregateId == aggregateId &&
                                               a.ETag == etag);
        }
    }
}