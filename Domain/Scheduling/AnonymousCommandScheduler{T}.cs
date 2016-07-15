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

        public AnonymousCommandScheduler(Func<IScheduledCommand<TAggregate>, Task> schedule)
        {
            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            this.schedule = schedule;
        }

        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand) =>
            await schedule(scheduledCommand);
    }
}