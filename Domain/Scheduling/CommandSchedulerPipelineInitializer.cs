// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A base class to simplify command scheduler initialization across all known command target types.
    /// </summary>
    /// <seealso cref="Microsoft.Its.Domain.ICommandSchedulerPipelineInitializer" />
    public abstract class CommandSchedulerPipelineInitializer : ICommandSchedulerPipelineInitializer
    {
        private readonly MethodInfo initializeFor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSchedulerPipelineInitializer"/> class.
        /// </summary>
        protected CommandSchedulerPipelineInitializer()
        {
            initializeFor = GetType().GetMethod("InitializeInternal", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Initializes the command scheduler in the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
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

        /// <summary>
        /// Gets a key indicating that the initializer has run.
        /// </summary>
        /// <returns></returns>
        protected internal virtual string GetKeyIndicatingInitialized() => GetType().ToString();

        /// <summary>
        /// Calls <see cref="InitializeFor{TAggregate}" /> for each known target type. This method supports the implementation of <see cref="CommandSchedulerPipelineInitializer" /> and is not intended to be called by inheriting classes.
        /// </summary>
        protected void InitializeInternal<TTarget>(Configuration configuration)
            where TTarget : class
        {
            var container = configuration.Container;

            var commandSchedulerPipeline = container.Resolve<CommandSchedulerPipeline<TTarget>>();
            var commandDelivererPipeline = container.Resolve<CommandDelivererPipeline<TTarget>>();

            container
                .Register(c => commandSchedulerPipeline)
                .Register(c => commandDelivererPipeline)
                .RegisterSingle(c => commandSchedulerPipeline.Compose(configuration))
                .RegisterSingle(c => commandDelivererPipeline.Compose(configuration));

            InitializeFor<TTarget>(configuration);
        }

        /// <summary>
        /// Initializes command scheduler pipeline behaviors for the specified command target type.
        /// </summary>
        /// <typeparam name="TTarget">The type of the command target.</typeparam>
        /// <param name="configuration">The configuration.</param>
        protected abstract void InitializeFor<TTarget>(Configuration configuration)
            where TTarget : class;
    }
}