// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    internal class AnonymousCommandScheduler<TAggregate> : ICommandScheduler<TAggregate>
    {
        private readonly Func<IScheduledCommand<TAggregate>, Task> schedule;
        private readonly Func<IScheduledCommand<TAggregate>, Task> deliver;

        public AnonymousCommandScheduler(Func<IScheduledCommand<TAggregate>, Task> schedule, Func<IScheduledCommand<TAggregate>, Task> deliver)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException("schedule");
            }
            if (deliver == null)
            {
                throw new ArgumentNullException("deliver");
            }
            this.schedule = schedule;
            this.deliver = deliver;
        }

        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await schedule(scheduledCommand);
        }

        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await deliver(scheduledCommand);
        }
    }
}