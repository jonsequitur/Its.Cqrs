// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// A <see cref="DbContext" /> that self-composes by discovering implementations of <see cref="IEntityModelConfiguration" />. 
    /// </summary>
    public class ReadModelDbContext : DbContext
    {
        static ReadModelDbContext()
        {
            Database.SetInitializer(new ReadModelDatabaseInitializer<ReadModelDbContext>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelDbContext"/> class.
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public ReadModelDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelDbContext"/> class.
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        /// <param name="model">The model that will back this context.</param>
        protected ReadModelDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        {
        }

        /// <summary>
        /// This method is called when the model for a derived context has been initialized, but
        ///                 before the model has been locked down and used to initialize the context.  The default
        ///                 implementation of this method does nothing, but it can be overridden in a derived class
        ///                 such that the model can be further configured before it is locked down.
        /// </summary>
        /// <remarks>
        /// Typically, this method is called only once when the first instance of a derived context
        ///                 is created.  The model for that context is then cached and is for all further instances of
        ///                 the context in the app domain.  This caching can be disabled by setting the ModelCaching
        ///                 property on the given ModelBuidler, but note that this can seriously degrade performance.
        ///                 More control over caching is provided through use of the DbModelBuilder and DbContextFactory
        ///                 classes directly.
        /// </remarks>
        /// <param name="modelBuilder">The builder that defines the model for the context being created.</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            foreach (var configuration in GetEntityModelConfigurationTypes()
                .Select(Domain.Configuration.Current.Container.Resolve)
                .Cast<IEntityModelConfiguration>().ToArray())
            {
                configuration.ConfigureModel(modelBuilder.Configurations);
            }
        }

        /// <summary>
        /// Gets the types of configurations to be used to configure the entity model for the read model database.
        /// </summary>
        /// <remarks>By default, this discovers and returns all types drived from <see cref="IEntityModelConfiguration" />.</remarks>
        protected virtual IEnumerable<Type> GetEntityModelConfigurationTypes()
        {
            return Discover.ConcreteTypesDerivedFrom(typeof (IEntityModelConfiguration));
        }
    }
}
