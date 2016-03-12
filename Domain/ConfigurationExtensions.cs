// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Gets an <see cref="IEventSourcedRepository{TAggregate}" />.
        /// </summary>
        public static IEventSourcedRepository<TAggregate> Repository<TAggregate>(this Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            return configuration.Container.Resolve<IEventSourcedRepository<TAggregate>>();
        }

        /// <summary>
        /// Gets an <see cref="IStore{TAggregate}" />.
        /// </summary>
        public static IStore<TTarget> Store<TTarget>(this Configuration configuration)
            where TTarget : class
        {
            return configuration.Container.Resolve<IStore<TTarget>>();
        }

        /// <summary>
        /// Gets an <see cref="ICommandScheduler{TAggregate}" />.
        /// </summary>
        public static ICommandScheduler<TAggregate> CommandScheduler<TAggregate>(this Configuration configuration)
            where TAggregate : class
        {
            return configuration.Container.Resolve<ICommandScheduler<TAggregate>>();
        }

        /// <summary>
        /// Configures the domain to use the specified event bus.
        /// </summary>
        public static Configuration UseEventBus(
            this Configuration configuration,
            IEventBus bus)
        {
            configuration.Container.RegisterSingle(c => bus);
            return configuration;
        }

        /// <summary>
        /// Enables Its.Domain to instantiate dependencies of command or event handlers.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="resolve">A delegate that will be called each time the specified type is needed by an object that Its.Domain is instantiating.</param>
        /// <returns></returns>
        /// <remarks>This method is called when Its.Domain instantiates an implementation of one of its interfaces (e.g. <see cref="ICommandHandler{TAggregate,TCommand}" />) that depends on a type that is owned by application code. The typical usage of this method is to wire up an IoC container that you've configured for your application.</remarks>
        public static Configuration UseDependency<T>(
            this Configuration configuration,
            Func<Func<Type, object>, T> resolve)
        {
            // ReSharper disable RedundantTypeArgumentsOfMethod
            configuration.Container.Register<T>(c => resolve(c.Resolve));
            // ReSharper restore RedundantTypeArgumentsOfMethod
            return configuration;
        }

        /// <summary>
        /// Enables Its.Domain to instantiate dependencies of command or event handlers.
        /// </summary>
        /// <remarks>
        ///  This method is called when Its.Domain instantiates an implementation of one of its interfaces (e.g. <see cref="ICommandHandler{TAggregate,TCommand}" />) that depends on a type that is owned by application code. The typical usage of this method is to wire up an IoC container that you've configured for your application.
        /// 
        /// Usage example:
        /// 
        /// <code>
        /// 
        /// MyContainer myContainer; 
        /// 
        /// configuration.UseDependencies(
        ///     type => {       
        ///        if (myContainer.IsRegistered(type)) 
        ///        {  
        ///             return () => myContainer.Resolve(type);
        ///        } 
        ///        
        ///        return null;
        /// 
        ///     });
        /// 
        /// </code>
        /// 
        /// </remarks>
        public static Configuration UseDependencies(
            this Configuration configuration,
            Func<Type, Func<object>> strategy)
        {
            configuration.Container
                         .AddStrategy(t =>
                         {
                             Func<object> resolveFunc = strategy(t);
                             if (resolveFunc != null)
                             {
                                 return container => resolveFunc();
                             }
                             return null;
                         });
            return configuration;
        }

        /// <summary>
        /// Writes trace information during command scheduling and delivery for all aggregate types. If no delegates are specified, then output is written to <see cref="System.Diagnostics.Trace" /> on all events.
        /// </summary>
        /// <param name="configuration">The domain configuration.</param>
        /// <param name="onScheduling">An optional delegate to trace information about a command before calling Schedule on the inner scheduler.</param>
        /// <param name="onScheduled">An optional delegate to trace information about a command after calling Schedule on the inner scheduler.</param>
        /// <param name="onDelivering">An optional delegate to trace information about a command before calling Deliver on the inner scheduler.</param>
        /// <param name="onDelivered">An optional delegate to trace information about a command after calling Deliver on the inner scheduler.</param>
        /// <returns>The same configuration object.</returns>
        public static Configuration TraceScheduledCommands(
            this Configuration configuration,
            Action<IScheduledCommand> onScheduling = null,
            Action<IScheduledCommand> onScheduled = null,
            Action<IScheduledCommand> onDelivering = null,
            Action<IScheduledCommand> onDelivered = null)
        {
            var traceInitializer = configuration.Container
                                                .Resolve<CommandSchedulerPipelineTraceInitializer>();

            if (onScheduling == null &&
                onScheduled == null &&
                onDelivering == null &&
                onDelivered == null)
            {
                onScheduling = cmd =>
                               Trace.WriteLine("[Scheduling] @" + Clock.Now() + ": " + cmd);

                onScheduled = cmd =>
                              Trace.WriteLine("[Scheduled] @" + Clock.Now() + ": " + cmd);

                onDelivering = cmd =>
                               Trace.WriteLine("[Delivering] @" + Clock.Now() + ": " + cmd);

                onDelivered = cmd =>
                              Trace.WriteLine("[Delivered] @" + Clock.Now() + ": " + cmd);
            }

            traceInitializer.OnScheduling(onScheduling);
            traceInitializer.OnScheduled(onScheduled);
            traceInitializer.OnDelivering(onDelivering);
            traceInitializer.OnDelivered(onDelivered);

            traceInitializer.Initialize(configuration);

            return configuration;
        }

        /// <summary>
        /// Adds a pipeline interceptor to command scheduler pipeline for a specific aggregate type.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="configuration">The configuration.</param>
        /// <param name="schedule">An optional delegate to intercept calls to Schedule.</param>
        /// <param name="deliver">An optional delegate to intercept calls to Deliver.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Legacy SQL command scheduler cannot be used with the command scheduler pipeline.</exception>
        public static Configuration AddToCommandSchedulerPipeline<TAggregate>(
            this Configuration configuration,
            ScheduledCommandInterceptor<TAggregate> schedule = null,
            ScheduledCommandInterceptor<TAggregate> deliver = null)
            where TAggregate : class
        {
            var pipeline = configuration.Container
                                        .Resolve<CommandSchedulerPipeline<TAggregate>>();

            if (schedule != null)
            {
                pipeline.OnSchedule(schedule);
            }
            if (deliver != null)
            {
                pipeline.OnDeliver(deliver);
            }

            configuration.Container
                         .Register(c => pipeline)
                         .RegisterSingle(c => pipeline.Compose(configuration));

            return configuration;
        }

        public static ISnapshotRepository SnapshotRepository(this Configuration configuration)
        {
            return configuration.Container.Resolve<ISnapshotRepository>();
        }

        internal static Configuration IsUsingInMemoryCommandScheduling(this Configuration configuration, bool value)
        {
            configuration.Properties["IsUsingInMemoryCommandScheduling"] = value;
            return configuration;
        }

        internal static bool IsUsingInMemoryCommandScheduling(this Configuration configuration)
        {
            return configuration.Properties
                                .IfContains("IsUsingInMemoryCommandScheduling")
                                .And()
                                .IfTypeIs<bool>()
                                .ElseDefault();
        }
    }
}