// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public class ImmediateCommandScheduler<TAggregate> : ICommandScheduler<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly IEventSourcedRepository<TAggregate> repository;

        public ImmediateCommandScheduler(IEventSourcedRepository<TAggregate> repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository;
        }

        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            var dueTime = scheduledCommand.DueTime;

            var domainNow = Clock.Current.Now();

            if (scheduledCommand.DeliveryPrecondition != null)
            {
                throw new InvalidOperationException("The ImmediateCommandScheduler does not support delivery preconditions.");
            }

            if (dueTime == null || dueTime <= domainNow)
            {
                await Deliver(scheduledCommand);
                return;
            }

            throw new InvalidOperationException("The ImmediateCommandScheduler does not support deferred scheduling.");
        }

        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            using (CommandContext.Establish(scheduledCommand.Command))
            {
                await repository.ApplyScheduledCommand(scheduledCommand);
            }
        }
    }
}
