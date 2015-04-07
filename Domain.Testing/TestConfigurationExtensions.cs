// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Its.Domain.Sql;
using Pocket;

namespace Microsoft.Its.Domain.Testing
{
    public static class TestConfigurationExtensions
    {
        /// <summary>
        /// Uses in memory command scheduling.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static Configuration UseInMemoryCommandScheduling(this Configuration configuration)
        {
            configuration.Container.RegisterGeneric(variantsOf: typeof (ICommandScheduler<>),
                                                    to: typeof (InMemoryCommandScheduler<>));

            AggregateType.KnownTypes.ForEach(t =>
            {
                var scheduler = configuration.Container.Resolve(typeof (ICommandScheduler<>).MakeGenericType(t));
                configuration.EventBus.Subscribe(scheduler);
            });

            return configuration;
        }

        public static Configuration IgnoreScheduledCommands(this Configuration configuration)
        {
            configuration.Container.RegisterGeneric(variantsOf: typeof (ICommandScheduler<>),
                                                    to: typeof (IgnoreCommandScheduling<>));
            return configuration;
        }

        public static Configuration UseInMemoryEventStore(this Configuration configuration)
        {
            configuration.Container
                         .RegisterSingle(c => new ConcurrentDictionary<string, IEventStream>(StringComparer.OrdinalIgnoreCase))
                         .AddStrategy(type => InMemoryEventSourcedRepositoryStrategy(type, configuration.Container));

            configuration.UsesSqlEventStore(false);

            return configuration;
        }

        internal static Func<PocketContainer, object> InMemoryEventSourcedRepositoryStrategy(Type type, PocketContainer container)
        {
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof (IEventSourcedRepository<>) ||
                 type.GetGenericTypeDefinition() == typeof (InMemoryEventSourcedRepository<>)))
            {
                var aggregateType = type.GenericTypeArguments.Single();
                var repositoryType = typeof (InMemoryEventSourcedRepository<>).MakeGenericType(aggregateType);

                var streamName = AggregateType.EventStreamName(aggregateType);

                // get the single registered event stream instance
                var stream = container.Resolve<ConcurrentDictionary<string, IEventStream>>()
                                      .GetOrAdd(streamName,
                                                name => container.Clone()
                                                                 .Register(_ => name)
                                                                 .Resolve<IEventStream>());

                return c => Activator.CreateInstance(repositoryType, stream, c.Resolve<IEventBus>());
            }

            if (type == typeof(IEventStream))
            {
                return c => c.Resolve<InMemoryEventStream>();
            }

            return null;
        }
    }
}
