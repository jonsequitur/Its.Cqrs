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
        public ImmediateCommandScheduler(IEventSourcedRepository<TAggregate> repository) : base(repository)
        {
        }

        public override async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
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
    }
}