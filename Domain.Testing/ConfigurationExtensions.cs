// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Testing
{
    public static class ConfigurationExtensions
    {
        public static async Task AndSave<TAggregate>(this Task<TAggregate> aggregate)
            where TAggregate : class, IEventSourced =>
                await Configuration.Current.Repository<TAggregate>().Save(await aggregate);

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

        internal static void EnsureCommandSchedulerPipelineTrackerIsInitialized(this Configuration configuration) =>
            TrackCommandsInPipeline(configuration);

        private static void TrackCommandsInPipeline(Configuration configuration) =>
            new CommandSchedulerPipelineTracker().Initialize(configuration);
    }
}