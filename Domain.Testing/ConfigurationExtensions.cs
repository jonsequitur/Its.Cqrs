// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Testing
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Triggers commands scheduled on the <see cref="SqlCommandScheduler" /> when a virtual clock is advanced.
        /// </summary>
        /// <param name="configuration">A domain configuration instance.</param>
        /// <returns>The modified domain configuration instance.</returns>
        [Obsolete("When using the command scheduler pipepline, VirtualClock integration is automatically enabled.")]
        public static Configuration TriggerSqlCommandSchedulerWithVirtualClock(this Configuration configuration)
        {
            if (configuration.IsUsingCommandSchedulerPipeline())
            {
                throw new InvalidOperationException("Only supported after configuring legacy SQL command scheduler by calling UseSqlCommandScheduler.");
            }

            var scheduler = configuration.SqlCommandScheduler();

            var subscription = scheduler.Activity
                                        .OfType<CommandScheduled>()
                                        .Subscribe(scheduled =>
                                        {
                                            Clock.Current
                                                 .IfTypeIs<VirtualClock>()
                                                 .ThenDo(clock => { clock.OnAdvanceTriggerSchedulerClock(scheduled.Clock); });
                                        });

            configuration.RegisterForDisposal(subscription);

            return configuration;
        }

        public static async Task AndSave<TAggregate>(this Task<TAggregate> aggregate)
            where TAggregate : class, IEventSourced
        {
            var repository = Configuration.Current.Repository<TAggregate>();
            await repository.Save(await aggregate);
        }

        public static void WriteInMemoryEventsToConsole(this Configuration configuration)
        {
            var streams = configuration
                .Container
                .Resolve<InMemoryEventStream>();

            var json = streams
                .Events
                .Select(e => new
                {
                    e.StreamName,
                    Event = e
                })
                .OrderBy(e => e.Event.Timestamp)
                .ToJson(Formatting.Indented);

            Console.WriteLine(json);
        }

        internal static void EnsureCommandSchedulerPipelineTrackerIsInitialized(this Configuration configuration)
        {
            if (!configuration.IsUsingCommandSchedulerPipeline())
            {
                return;
            }

            AggregateType.KnownTypes.ForEach(aggregateType =>
            {
                var initializerType = typeof (PipelineTrackerFor<>).MakeGenericType(aggregateType);

                var initializer = configuration.Container.Resolve(initializerType) as ISchedulerPipelineInitializer;

                initializer.Initialize(configuration);
            });
        }

        internal class PipelineTrackerFor<TAggregate> :
            ISchedulerPipelineInitializer where TAggregate : class, IEventSourced
        {
            private static readonly object lockObj = new object();

            public void Initialize(Configuration configuration)
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

            internal static CommandsInPipeline TrackCommandsInPipeline(
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

        internal static void IsUsingInMemoryCommandScheduling(this Configuration configuration, bool value)
        {
            configuration.Properties["IsUsingInMemoryCommandScheduling"] = value;
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