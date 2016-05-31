// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;
using Pocket;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for preparing the domain configuration. 
    /// </summary>
    public static class ConfigurationExtensions
    {
        public static ISchedulerClockRepository SchedulerClockRepository(this Configuration configuration) =>
            configuration.Container.Resolve<ISchedulerClockRepository>();

        public static ISchedulerClockTrigger SchedulerClockTrigger(this Configuration configuration) =>
            configuration.Container.Resolve<ISchedulerClockTrigger>();

        /// <summary>
        /// Configures the system to use a SQL-based event store.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="createEventStoreDbContext">A function that returns an <see cref="EventStoreDbContext" />, if something different from the default is desired.</param>
        /// <returns>The updated configuration.</returns>
        public static Configuration UseSqlEventStore(
            this Configuration configuration,
            Func<EventStoreDbContext> createEventStoreDbContext = null)
        {
            configuration.Container.AddStrategy(SqlEventSourcedRepositoryStrategy);
            configuration.IsUsingSqlEventStore(true);

            createEventStoreDbContext = createEventStoreDbContext ??
                                        (() => new EventStoreDbContext());

            configuration.Container
                         .Register(c => createEventStoreDbContext())
                         .Register<IETagChecker>(c => c.Resolve<SqlEventStoreEventStoreETagChecker>());

            return configuration;
        }

        public static Configuration UseSqlReservationService(this Configuration configuration)
        {
            configuration.Container.Register<IReservationService>(c => new SqlReservationService());
            return configuration;
        }

        public static Configuration UseSqlStorageForScheduledCommands(
            this Configuration configuration)
        {
            configuration.IsUsingInMemoryCommandScheduling(false);

            var container = configuration.Container;

            container.RegisterDefaultClockName()
                     .Register<ISchedulerClockRepository>(
                         c => c.Resolve<SchedulerClockRepository>())
                     .Register<IETagChecker>(
                         c => c.Resolve<SqlEventStoreEventStoreETagChecker>())
                     .Register<ISchedulerClockTrigger>(
                         c => c.Resolve<SchedulerClockTrigger>());

            configuration.Container
                         .Resolve<SqlCommandSchedulerPipelineInitializer>()
                         .Initialize(configuration);

            var commandSchedulerResolver = new CommandSchedulerResolver(container);

            container
                .Register(
                    c => new SchedulerClockTrigger(
                             c.Resolve<CommandSchedulerDbContext>,
                             async (serializedCommand, result, db) =>
                             {
                                 await DeserializeAndDeliver(commandSchedulerResolver, serializedCommand, db);

                                 result.Add(serializedCommand.Result);
                             }))
                .Register<ISchedulerClockTrigger>(c => c.Resolve<SchedulerClockTrigger>());

            return configuration;
        }

        private static async Task DeserializeAndDeliver(
            CommandSchedulerResolver schedulerResolver,
            ScheduledCommand serializedCommand,
            CommandSchedulerDbContext db)
        {
            dynamic scheduler = schedulerResolver.ResolveSchedulerForAggregateTypeNamed(serializedCommand.AggregateType);

            await Storage.DeserializeAndDeliverScheduledCommand(
                serializedCommand,
                scheduler);

            serializedCommand.Attempts++;

            await db.SaveChangesAsync();
        }

        public static async Task DeserializeAndDeliver(
            this Configuration configuration,
            ScheduledCommand serializedCommand,
            CommandSchedulerDbContext db)
        {
            var resolver = configuration.Container.Resolve<CommandSchedulerResolver>();
            await DeserializeAndDeliver(resolver, serializedCommand, db);
        }

        internal static void IsUsingSqlEventStore(this Configuration configuration, bool value) =>
            configuration.Properties["IsUsingSqlEventStore"] = value;

        internal static bool IsUsingSqlEventStore(this Configuration configuration) =>
            configuration.Properties
                         .IfContains("IsUsingSqlEventStore")
                         .And()
                         .IfTypeIs<bool>()
                         .ElseDefault();

        internal static Func<PocketContainer, object> SqlEventSourcedRepositoryStrategy(Type type)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof (IEventSourcedRepository<>))
            {
                var aggregateType = type.GenericTypeArguments.Single();
                var genericType = typeof (SqlEventSourcedRepository<>).MakeGenericType(aggregateType);
                return c => c.Resolve(genericType);
            }
            return null;
        }

        internal static PocketContainer RegisterDefaultClockName(this PocketContainer container) =>
            container.AddStrategy(t =>
            {
                if (t == typeof (GetClockName))
                {
                    return c => new GetClockName(e => CommandScheduler.Clock.DefaultClockName);
                }

                return null;
            });
    }
}