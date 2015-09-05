// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public delegate Task ScheduledCommandPipelineDelegate<TAggregate>(
        IScheduledCommand<TAggregate> command,
        Func<IScheduledCommand<TAggregate>, Task> next) where TAggregate : IEventSourced;

    internal class CommandSchedulerPipeline<TAggregate>
        where TAggregate : class, IEventSourced
    {
        private readonly List<ScheduledCommandPipelineDelegate<TAggregate>> onSchedule = new List<ScheduledCommandPipelineDelegate<TAggregate>>();

        private readonly List<ScheduledCommandPipelineDelegate<TAggregate>> onDeliver = new List<ScheduledCommandPipelineDelegate<TAggregate>>();

        public void OnSchedule(ScheduledCommandPipelineDelegate<TAggregate> segment)
        {
            onSchedule.Insert(0, segment);
        }

        public void OnDeliver(ScheduledCommandPipelineDelegate<TAggregate> segment)
        {
            onDeliver.Insert(0, segment);
        }

        public ICommandScheduler<TAggregate> Compose(Configuration configuration)
        {
            var scheduler = configuration.Container.Resolve<CommandScheduler<TAggregate>>();

            return scheduler
                .Wrap(onSchedule.Compose(),
                      onDeliver.Compose());
        }
    }
}