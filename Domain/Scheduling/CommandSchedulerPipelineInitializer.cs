// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Its.Domain
{
    public abstract class CommandSchedulerPipelineInitializer : ICommandSchedulerPipelineInitializer
    {
        private readonly MethodInfo initializeFor;

        protected CommandSchedulerPipelineInitializer()
        {
            initializeFor = GetType().GetMethod("InitializeInternal", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Initialize(Configuration configuration) =>
            configuration.Properties.GetOrAdd(
                GetKeyIndicatingInitialized(),
                _ =>
                {
                    Command.KnownTargetTypes
                        .ForEach(type =>
                        {
                            var method = initializeFor.MakeGenericMethod(type);
                            method.Invoke(this, new[] { configuration });
                        });
                    return true;
                });

        protected internal virtual string GetKeyIndicatingInitialized() => GetType().ToString();

        protected void InitializeInternal<TAggregate>(Configuration configuration)
            where TAggregate : class
        {
            var container = configuration.Container;

            var commandSchedulerPipeline = container.Resolve<CommandSchedulerPipeline<TAggregate>>();
            var commandDelivererPipeline = container.Resolve<CommandDelivererPipeline<TAggregate>>();

            container
                .Register(c => commandSchedulerPipeline)
                .Register(c => commandDelivererPipeline)
                .RegisterSingle(c => commandSchedulerPipeline.Compose(configuration))
                .RegisterSingle(c => commandDelivererPipeline.Compose(configuration));

            InitializeFor<TAggregate>(configuration);
        }

        protected abstract void InitializeFor<TAggregate>(Configuration configuration)
            where TAggregate : class;
    }
}