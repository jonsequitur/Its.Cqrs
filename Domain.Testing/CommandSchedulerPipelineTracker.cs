// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection;
using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Testing
{
    internal abstract class SchedulerPipelineInitializer : ISchedulerPipelineInitializer
    {
        private readonly MethodInfo initializeFor;

        protected SchedulerPipelineInitializer()
        {
            initializeFor = GetType().GetMethod("InitializeFor", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Initialize(Configuration configuration)
        {
            AggregateType.KnownTypes.ForEach(aggregateType =>
            {
                initializeFor.MakeGenericMethod(aggregateType).Invoke(this, new[] { configuration });
            });
        }

        protected abstract void InitializeFor<TAggregate>(Configuration configuration)
            where TAggregate : class, IEventSourced;
    }

    internal class CommandSchedulerPipelineTracker : SchedulerPipelineInitializer
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