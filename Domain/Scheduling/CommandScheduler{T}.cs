// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public class CommandScheduler<TAggregate> : ICommandScheduler<TAggregate> where TAggregate : class, IEventSourced
    {
        protected readonly IEventSourcedRepository<TAggregate> repository;

        public CommandScheduler(IEventSourcedRepository<TAggregate> repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository;
        }

        public virtual Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            throw new NotSupportedException("Schedule is not supported.");
        }

        public virtual async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            // FIX: (Deliver) add a precondition check here

            using (CommandContext.Establish(scheduledCommand.Command))
            {
                await repository.ApplyScheduledCommand(scheduledCommand);
            }
        }
    }
}