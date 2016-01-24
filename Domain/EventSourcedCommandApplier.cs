// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal class EventSourcedCommandApplier<TTarget> : ICommandApplier<TTarget>
        where TTarget : class, IEventSourced
    {
        private readonly IEventSourcedRepository<TTarget> repository;
        private readonly ICommandPreconditionVerifier preconditionVerifier;

        public EventSourcedCommandApplier(
            IEventSourcedRepository<TTarget> repository,
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

        public async Task ApplyScheduledCommand(IScheduledCommand<TTarget> scheduledCommand)
        {
            await repository.ApplyScheduledCommand(scheduledCommand, preconditionVerifier);
        }
    }

    internal class DefaultCommandApplier<TTarget> : ICommandApplier<TTarget>
        where TTarget : class
    {
        public async Task ApplyScheduledCommand(IScheduledCommand<TTarget> scheduledCommand)
        {
        }
    }
}