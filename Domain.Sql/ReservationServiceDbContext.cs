// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// A database context for interacting with a reservation service SQL-based backing store.
    /// </summary>
    public class ReservationServiceDbContext : DbContext
    {
        /// <summary>
        /// Initializes the <see cref="ReservationServiceDbContext"/> class.
        /// </summary>
        static ReservationServiceDbContext()
        {
            Database.SetInitializer(new ReservationServiceDatabaseInitializer());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReservationServiceDbContext"/> class.
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public ReservationServiceDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        /// <summary>
        /// This method is called when the model for a derived context has been initialized, but
        /// before the model has been locked down and used to initialize the context.  The default
        /// implementation of this method does nothing, but it can be overridden in a derived class
        /// such that the model can be further configured before it is locked down.
        /// </summary>
        /// <remarks>
        /// Typically, this method is called only once when the first instance of a derived context
        /// is created.  The model for that context is then cached and is for all further instances of
        /// the context in the app domain.  This caching can be disabled by setting the ModelCaching
        /// property on the given ModelBuidler, but note that this can seriously degrade performance.
        /// More control over caching is provided through use of the DbModelBuilder and DbContextFactory
        /// classes directly.
        /// </remarks>
        /// <param name="modelBuilder"> The builder that defines the model for the context being created. </param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new ReservedValuesEntityTypeConfiguration());
        }

        /// <summary>
        /// Gets or sets the reserved values.
        /// </summary>
        public DbSet<ReservedValue> ReservedValues { get; set; }

        private class ReservedValuesEntityTypeConfiguration : EntityTypeConfiguration<ReservedValue>
        {
            public ReservedValuesEntityTypeConfiguration()
            {
                ToTable("ReservedValues", "Reservations");

                HasKey(r => new { r.Value, r.Scope });

                Property(r => r.OwnerToken)
                    .IsRequired();
                Property(r => r.Expiration)
                    .IsConcurrencyToken();
                Property(r => r.ConfirmationToken)
                    .HasMaxLength(256);
            }
        }
    }
}
