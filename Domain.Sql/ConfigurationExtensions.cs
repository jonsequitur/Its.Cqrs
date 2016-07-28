// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
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
        /// <summary>
        /// Deserializes a scheduled command from SQL storage and delivers it to the target.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="serializedCommand">The serialized command.</param>
        /// <param name="db">The command scheduler database context.</param>
        public static async Task DeserializeAndDeliver(
            this Configuration configuration,
            ScheduledCommand serializedCommand,
            CommandSchedulerDbContext db) =>
                await DeserializeAndDeliver(
                    configuration.Container.Resolve<CommandDelivererResolver>(),
                    serializedCommand,
                    db);

        internal static async Task DeserializeAndDeliver(
            CommandDelivererResolver delivererResolver,
            ScheduledCommand serializedCommand,
            CommandSchedulerDbContext db)
        {
            dynamic scheduler = delivererResolver.ResolveSchedulerForAggregateTypeNamed(serializedCommand.AggregateType);

            await Storage.DeserializeAndDeliverScheduledCommand(
                serializedCommand,
                scheduler);

            serializedCommand.Attempts++;

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a db context that can be used to work with the command scheduler database.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static CommandSchedulerDbContext CommandSchedulerDbContext(
            this Configuration configuration)
        {
            try
            {
                return configuration.Container.Resolve<CommandSchedulerDbContext>();
            }
            catch (TargetInvocationException)
            {
                throw new DomainConfigurationException("Command scheduler database is not configured.");
            }
        }

        /// <summary>
        /// Gets a db context that can be used to work with the event store.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static EventStoreDbContext EventStoreDbContext(
            this Configuration configuration)
        {
            try
            {
                return configuration.Container.Resolve<EventStoreDbContext>();
            }
            catch (TargetInvocationException)
            {
                throw new DomainConfigurationException("Event store is not configured.");
            }
        }

        /// <summary>
        /// Gets a db context that can be used to work with read models.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        internal static ReadModelDbContext ReadModelDbContext(
            this Configuration configuration)
        {
            try
            {
                return configuration.Container.Resolve<ReadModelDbContext>();
            }
            catch (TargetInvocationException)
            {
                throw new DomainConfigurationException("ReadModelDbContext is not configured.");
            }
        }

        /// <summary>
        /// Gets a scheduler clock repository.
        /// </summary>
        public static ISchedulerClockRepository SchedulerClockRepository(this Configuration configuration) =>
            configuration.Container.Resolve<ISchedulerClockRepository>();

        /// <summary>
        /// Gets a scheduler clock trigger.
        /// </summary>
        public static ISchedulerClockTrigger SchedulerClockTrigger(this Configuration configuration) =>
            configuration.Container.Resolve<ISchedulerClockTrigger>();

        /// <summary>
        /// Configures the system to use a SQL-based event store.
        /// </summary>
        /// <returns>The updated configuration.</returns>
        public static Configuration UseSqlEventStore(
            this Configuration configuration,
            Action<EventStoreConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var schedulerConfiguration = new EventStoreConfiguration();
            configure(schedulerConfiguration);
            schedulerConfiguration.ApplyTo(configuration);
            return configuration;
        }

        /// <summary>
        /// Configures the use of a SQL-backed reservation service. 
        /// </summary>
        public static Configuration UseSqlReservationService(
            this Configuration configuration,
            Action<ReservationServiceConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var reservationServiceConfiguration = new ReservationServiceConfiguration();
            configure(reservationServiceConfiguration);
            reservationServiceConfiguration.ApplyTo(configuration);
            return configuration;
        }

        /// <summary>
        /// Configures the use of a SQL-backed command scheduler. 
        /// </summary>
        public static Configuration UseSqlStorageForScheduledCommands(
            this Configuration configuration,
            Action<CommandSchedulerConfiguration> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));
            var schedulerConfiguration = new CommandSchedulerConfiguration();
            configure.Invoke(schedulerConfiguration);
            schedulerConfiguration.ApplyTo(configuration);

            return configuration;
        }

        internal static void IsUsingSqlEventStore(this Configuration configuration, bool value) =>
            configuration.Properties["IsUsingSqlEventStore"] = value;

        internal static bool IsUsingSqlEventStore(this Configuration configuration) =>
            configuration.Properties
                         .IfContains("IsUsingSqlEventStore")
                         .And()
                         .IfTypeIs<bool>()
                         .ElseDefault();

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