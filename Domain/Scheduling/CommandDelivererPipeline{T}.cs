// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    internal class CommandDelivererPipeline<TAggregate>
        where TAggregate : class
    {
        private readonly List<ScheduledCommandInterceptor<TAggregate>> onDeliver = new List<ScheduledCommandInterceptor<TAggregate>>();

        public void OnDeliver(ScheduledCommandInterceptor<TAggregate> segment) =>
            onDeliver.Insert(0, segment);

        public ICommandDeliverer<TAggregate> Compose(Configuration configuration) =>
            configuration.Container
                .Resolve<CommandScheduler<TAggregate>>()
                .InterceptDeliver(onDeliver.Compose());
    }
}