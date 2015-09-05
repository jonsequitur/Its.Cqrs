// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public class ImmediateCommandScheduler<TAggregate> :
        CommandScheduler<TAggregate>
        where TAggregate : class, IEventSourced
    {
        public ImmediateCommandScheduler(
            IEventSourcedRepository<TAggregate> repository,
            ICommandPreconditionVerifier preconditionVerifier = null) : base(repository, preconditionVerifier)
        {
        }

        public override async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.DeliveryPrecondition != null)
            {
                throw new InvalidOperationException("The ImmediateCommandScheduler does not support delivery preconditions.");
            }

            if (scheduledCommand.IsDue())
            {
                await Configuration.Current.CommandScheduler<TAggregate>().Deliver(scheduledCommand);
                return;
            }

            throw new InvalidOperationException("The ImmediateCommandScheduler does not support deferred scheduling.");
        }
    }
}