// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides access to a SQL-based event store.
    /// </summary>
    public class EventStoreDbContext : DbContext
    {
        private static string nameOrConnectionString;

        static EventStoreDbContext()
        {
            Database.SetInitializer(new EventStoreDatabaseInitializer<EventStoreDbContext>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDbContext"/> class.
        /// </summary>
        public EventStoreDbContext() : this(NameOrConnectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDbContext"/> class.
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public EventStoreDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StorableEvent>()
                        .HasKey(x => new { x.AggregateId, x.SequenceNumber })
                        .ToTable("Events", "EventStore");

            modelBuilder.Entity<StorableEvent>()
                        .Property(e => e.Body);

            modelBuilder.Entity<StorableEvent>()
                        .Property(e => e.Type)
                        .IsRequired();

            modelBuilder.Entity<StorableEvent>()
                        .Property(e => e.StreamName)
                        .IsRequired();

            modelBuilder.Entity<StorableEvent>()
                        .Property(e => e.Id)
                        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // ignore timestamp since DateTimeOffset is not supported by EF in some stores
            modelBuilder.Entity<StorableEvent>().Ignore(a => a.Timestamp);
        }

        public DbSet<StorableEvent> Events { get; set; }

        public static string NameOrConnectionString
        {
            get
            {
                return nameOrConnectionString;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("The value cannot be null, empty or contain only whitespace.");
                }
                nameOrConnectionString = value;
            }
        }
    }
}
