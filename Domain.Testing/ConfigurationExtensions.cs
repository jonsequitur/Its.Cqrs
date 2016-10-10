// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Provides methods for working with domain configuration.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Saves an aggregate using the currently configured repository.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        public static async Task AndSave<TAggregate>(this Task<TAggregate> aggregate)
            where TAggregate : class, IEventSourced =>
                await Configuration.Current.Repository<TAggregate>().Save(await aggregate);

        /// <summary>
        /// Gets the default clock name.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static string DefaultClockName(this Configuration configuration) =>
            configuration.Container.Resolve<GetClockName>()(null);

        /// <summary>
        /// Writes all events currently stored in the in-memory event store out to the console.
        /// </summary>
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

        /// <summary>
        /// Gets the in-memory event stream for the current configuration.
        /// </summary>
        public static InMemoryEventStream InMemoryEventStream(this Configuration configuration) =>
            configuration.Container.Resolve<InMemoryEventStream>();

        internal static void EnsureCommandSchedulerPipelineTrackerIsInitialized(this Configuration configuration) =>
            TrackCommandsInPipeline(configuration);

        private static void TrackCommandsInPipeline(Configuration configuration) =>
            new CommandSchedulerPipelineTracker().Initialize(configuration);
    }
}