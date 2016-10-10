// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Pocket;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Configures interaction with SQL-based event stores.
    /// </summary>
    public class EventStoreConfiguration
    {
        private readonly IList<Action<Configuration>> configureActions = new List<Action<Configuration>>();

        /// <summary>
        /// Specifies the database connection string.
        /// </summary>
        public EventStoreConfiguration UseConnectionString(
            string connectionString) =>
                UseDbContext(() => new EventStoreDbContext(connectionString));

        /// <summary>
        /// Specifies a delegate to be called when creating database contexts for the event store.
        /// </summary>
        public EventStoreConfiguration UseDbContext(
            Func<EventStoreDbContext> create)
        {
            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            configureActions.Add(configuration => configuration.Container.Register(_ => create()));

            return this;
        }

        internal void ApplyTo(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Container.AddStrategy(SqlEventSourcedRepositoryStrategy);
            configuration.IsUsingSqlEventStore(true);

            configuration.Container
                         .Register<IETagChecker>(c => c.Resolve<SqlEventStoreETagChecker>());

            foreach (var configure in configureActions)
            {
                configure(configuration);
            }
        }

        private static Func<PocketContainer, object> SqlEventSourcedRepositoryStrategy(Type type)
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
    }
}