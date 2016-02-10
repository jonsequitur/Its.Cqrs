// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Its.Domain.Testing
{
    internal class InMemoryCommandSchedulerPipelineInitializer : CommandSchedulerPipelineInitializer
    {
        protected override void InitializeFor<TAggregate>(Configuration configuration)
        {
            configuration.IsUsingCommandSchedulerPipeline(true)
                         .IsUsingInMemoryCommandScheduling(true);

            configuration.Container.RegisterSingle(c => new InMemoryCommandETagStore());

            configuration.AddToCommandSchedulerPipeline(
                CommandScheduler.WithInMemoryDeferredScheduling<TAggregate>(configuration));
        }
    }
}