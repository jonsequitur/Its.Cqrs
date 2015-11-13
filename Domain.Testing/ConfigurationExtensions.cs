// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
            if (!configuration.IsUsingLegacySqlCommandScheduling())
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
                                                 .ThenDo(clock =>
                                                 {
                                                     clock.OnAdvanceTriggerSchedulerClock(scheduled.Clock);
                                                 });
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
    }
}