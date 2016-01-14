// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class EventSourcedCommandApplier<TAggregate> : ICommandApplier<TAggregate> where TAggregate : class, IEventSourced
    {
        private readonly IEventSourcedRepository<TAggregate> repository;
        private readonly ICommandPreconditionVerifier preconditionVerifier;

        public EventSourcedCommandApplier(
            IEventSourcedRepository<TAggregate> repository, 
            ICommandPreconditionVerifier preconditionVerifier)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            if (preconditionVerifier == null)
            {
                throw new ArgumentNullException("preconditionVerifier");
            }

            this.repository = repository;
            this.preconditionVerifier = preconditionVerifier;
        }

        public async Task ApplyScheduledCommand(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await repository.ApplyScheduledCommand(scheduledCommand, preconditionVerifier);
        }
    }
}