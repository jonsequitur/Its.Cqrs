// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    internal class CommandSchedulerPipeline<TAggregate>
        where TAggregate : class
    {
        private readonly List<ScheduledCommandInterceptor<TAggregate>> onSchedule = new List<ScheduledCommandInterceptor<TAggregate>>();

        private readonly List<ScheduledCommandInterceptor<TAggregate>> onDeliver = new List<ScheduledCommandInterceptor<TAggregate>>();

        public void OnSchedule(ScheduledCommandInterceptor<TAggregate> segment) =>
            onSchedule.Insert(0, segment);

        public void OnDeliver(ScheduledCommandInterceptor<TAggregate> segment) =>
            onDeliver.Insert(0, segment);

        public ICommandScheduler<TAggregate> Compose(Configuration configuration) =>
            configuration.Container
                         .Resolve<CommandScheduler<TAggregate>>()
                         .Wrap(onSchedule.Compose(),
                               onDeliver.Compose());
    }
}