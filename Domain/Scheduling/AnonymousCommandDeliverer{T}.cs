// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    [DebuggerStepThrough]
    internal class AnonymousCommandDeliverer<TAggregate> : ICommandDeliverer<TAggregate>
    {
        private readonly Func<IScheduledCommand<TAggregate>, Task> deliver;

        public AnonymousCommandDeliverer(Func<IScheduledCommand<TAggregate>, Task> deliver)
        {
            if (deliver == null)
            {
                throw new ArgumentNullException(nameof(deliver));
            }

            this.deliver = deliver;
        }

        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand) =>
            await deliver(scheduledCommand);
    }
}