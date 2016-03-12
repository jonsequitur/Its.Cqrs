// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Testing
{
    internal class IgnoreCommandScheduling<TAggregate> : ICommandScheduler<TAggregate>
        where TAggregate : IEventSourced
    {
        public Task Schedule(IScheduledCommand<TAggregate> scheduledCommand) => Task.FromResult(Unit.Default);

        public Task Deliver(IScheduledCommand<TAggregate> scheduledCommand) => Task.FromResult(Unit.Default);
    }
}