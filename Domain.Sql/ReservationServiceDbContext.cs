// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;

namespace Microsoft.Its.Domain.Sql
{
    public class ReservationServiceDbContext : DbContext
    {
        private static string nameOrConnectionString;

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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new ReservedValuesEntityTypeConfiguration());
        }

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
