// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Its.Domain.Testing
{
    internal class CommandSchedulerPipelineTracker : CommandSchedulerPipelineInitializer
    {
        private static readonly object lockObj = new object();

        protected override void InitializeFor<TAggregate>(Configuration configuration)
        {
            var commandsInPipeline = TrackCommandsInPipeline(configuration);

            configuration.AddToCommandSchedulerPipeline<TAggregate>(
                schedule: async (command, next) =>
                {
                    commandsInPipeline.Add(command);
                    await next(command);
                },
                deliver: async (command, next) =>
                {
                    await next(command);
                    commandsInPipeline.Remove(command);
                });
        }

        private static CommandsInPipeline TrackCommandsInPipeline(
            Configuration configuration)
        {
            // resolve and register so there's only a single instance registered at any given time
            CommandsInPipeline inPipeline;

            lock (lockObj)
            {
                inPipeline = configuration.Container.Resolve<CommandsInPipeline>();
                configuration.Container.Register(c => inPipeline);
            }

            return inPipeline;
        }
    }
}