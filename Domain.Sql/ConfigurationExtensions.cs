// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
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
            configuration.UsesSqlEventStore(true);

            createEventStoreDbContext = createEventStoreDbContext ??
                                        (() => new EventStoreDbContext());

            configuration.Container
                         .Register(c => createEventStoreDbContext())
                         .Register<ICommandPreconditionVerifier>(c => c.Resolve<CommandPreconditionVerifier>());

            return configuration;
        }

        /// <summary>
        /// Configures the system to use SQL-backed command scheduling.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The updated configuration.</returns>
        public static Configuration UseSqlCommandScheduling(
            this Configuration configuration,
            Action<ReadModelCatchup<CommandSchedulerDbContext>> configureCatchup = null)
        {
            var container = configuration.Container;
            var scheduler = container.Resolve<SqlCommandScheduler>();
            var subscription = container.Resolve<IEventBus>().Subscribe(scheduler);
            configuration.RegisterForDisposal(subscription);
            container.RegisterSingle(c => scheduler);

            if (configureCatchup != null)
            {
                var catchup = new ReadModelCatchup<CommandSchedulerDbContext>(scheduler)
                {
                    CreateReadModelDbContext = scheduler.CreateCommandSchedulerDbContext
                };
                configureCatchup(catchup);
                catchup.PollEventStore();
                container.RegisterSingle(c => catchup);
                configuration.RegisterForDisposal(catchup);
            }

            configuration.UsesSqlCommandScheduling(true);

            return configuration;
        }

        public static SqlCommandScheduler SqlCommandScheduler(this Configuration configuration)
        {
            if (!configuration.UsesSqlCommandScheduling())
            {
                throw new InvalidOperationException("You must first call UseSqlCommandScheduling to enable the use of the SqlCommandScheduler.");
            }
            return configuration.Container.Resolve<SqlCommandScheduler>();
        }

        internal static void UsesSqlCommandScheduling(this Configuration configuration, bool value)
        {
            configuration.Properties["UsesSqlCommandScheduling"] = value;
        }

        internal static bool UsesSqlCommandScheduling(this Configuration configuration)
        {
            return configuration.Properties
                                .IfContains("UsesSqlCommandScheduling")
                                .And()
                                .IfTypeIs<bool>()
                                .ElseDefault();
        }

         internal static void UsesSqlEventStore(this Configuration configuration, bool value)
        {
            configuration.Properties["UsesSqlEventStore"] = value;
        }

        internal static bool UsesSqlEventStore(this Configuration configuration)
        {
            return configuration.Properties
                                .IfContains("UsesSqlEventStore")
                                .And()
                                .IfTypeIs<bool>()
                                .ElseDefault();
        }

        internal static Func<PocketContainer, object> SqlEventSourcedRepositoryStrategy(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (IEventSourcedRepository<>))
            {
                var aggregateType = type.GenericTypeArguments.Single();
                var genericType = typeof (SqlEventSourcedRepository<>).MakeGenericType(aggregateType);
                return c => c.Resolve(genericType);
            }
            return null;
        }
    }
}
