// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Newtonsoft.Json;
using Pocket;

namespace Microsoft.Its.Domain.Testing
{
    public static class TestConfigurationExtensions
    {
        /// <summary>
        /// Sets up in-memory command scheduling for all known aggregate types.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static Configuration UseInMemoryCommandScheduling(this Configuration configuration)
        {
            configuration.IsUsingInMemoryCommandScheduling(true);

            configuration.Container
                         .Register<IETagChecker>(c => c.Resolve<InMemoryEventStoreETagChecker>());

            configuration.Container
                         .Resolve<InMemoryCommandSchedulerPipelineInitializer>()
                         .Initialize(configuration);

            return configuration;
        }

        public static Configuration IgnoreScheduledCommands(this Configuration configuration)
        {
            configuration.Container.RegisterGeneric(variantsOf: typeof (ICommandScheduler<>),
                                                    to: typeof (IgnoreCommandScheduling<>));
            return configuration;
        }

        public static Configuration UseInMemoryCommandTargetStore(this Configuration configuration)
        {
            configuration.Container
                         .AddStrategy(type =>
                         {
                             if (!type.IsGenericType ||
                                 type.GetGenericTypeDefinition() != typeof (IStore<>))
                             {
                                 return null;
                             }

                             var targetType = type.GetGenericArguments().Single();

                             if (typeof(IEventSourced).IsAssignableFrom(targetType))
                             {
                                 return null;
                             }

                             var storeType = typeof (InMemoryStore<>).MakeGenericType(targetType);

                             var store = Activator.CreateInstance(storeType,
                                                                  new object[] { (dynamic) null, (dynamic) null });

                             return c => store;
                         });

            return configuration;
        }

        public static Configuration UseInMemoryEventStore(
            this Configuration configuration,
            bool traceEvents = false)
        {
            var inMemoryEventStream = configuration.Container.Resolve<InMemoryEventStream>();

            configuration.Container
                         .AddStrategy(type => InMemoryEventSourcedRepositoryStrategy(type, configuration.Container))
                         .Register<IETagChecker>(c => c.Resolve<InMemoryEventStoreETagChecker>())
                         .RegisterSingle(c => inMemoryEventStream)
                         .RegisterSingle<ISnapshotRepository>(c => new InMemorySnapshotRepository());

            if (traceEvents)
            {
                var tracingSubscription = configuration.EventBus
                                                       .Events<IEvent>()
                                                       .Subscribe(TraceEvent);
                configuration.RegisterForDisposal(tracingSubscription);
            }

            configuration.IsUsingSqlEventStore(false);

            return configuration;
        }

        private static void TraceEvent(IEvent e)
        {
            Trace.WriteLine($"{e.EventStreamName()}.{e.EventName()}");
            Trace.WriteLine(
                e.ToJson(Formatting.Indented)
                 .Split('\n')
                 .Select(line => "   " + line)
                 .ToDelimitedString("\n"));
        }

        internal static Func<PocketContainer, object> InMemoryEventSourcedRepositoryStrategy(Type type, PocketContainer container)
        {
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof (IEventSourcedRepository<>) ||
                 type.GetGenericTypeDefinition() == typeof (InMemoryEventSourcedRepository<>)))
            {
                var aggregateType = type.GenericTypeArguments.Single();
                var repositoryType = typeof (InMemoryEventSourcedRepository<>).MakeGenericType(aggregateType);
                return c => Activator.CreateInstance(repositoryType,
                                                     c.Resolve<InMemoryEventStream>(),
                                                     c.Resolve<IEventBus>());
            }

            return null;
        }

        public static Configuration UseInMemoryReservationService(this Configuration configuration)
        {
            var inMemoryReservationService = new InMemoryReservationService();
            configuration.Container.RegisterSingle<IReservationService>(c => inMemoryReservationService);
            return configuration;
        }
    }
}