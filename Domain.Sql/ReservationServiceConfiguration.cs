// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides configuration for a SQL-based reservation service.
    /// </summary>
    public class ReservationServiceConfiguration
    {
        private readonly IList<Action<Configuration>> configureActions = new List<Action<Configuration>>();

        /// <summary>
        /// Specifies the connection string for the reservation service database.
        /// </summary>
        public ReservationServiceConfiguration UseConnectionString(
                string connectionString) =>
            UseDbContext(() => new ReservationServiceDbContext(connectionString));

        /// <summary>
        /// Specifies how to create instances of <see cref="ReservationServiceDbContext" />.
        /// </summary>
        /// <param name="create">A delegate that creates the db context.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public ReservationServiceConfiguration UseDbContext(
            Func<ReservationServiceDbContext> create)
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

            configuration.Container
                         .Register<IReservationService>(
                             c => c.Resolve<SqlReservationService>());

            foreach (var configure in configureActions)
            {
                configure(configuration);
            }
        }
    }
}