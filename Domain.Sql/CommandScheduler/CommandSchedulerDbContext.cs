// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Diagnostics;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    /// <summary>
    /// A database context for interacting with a command scheduler's SQL-based backing store.
    /// </summary>
    /// <seealso cref="System.Data.Entity.DbContext" />
    [DebuggerStepThrough]
    public class CommandSchedulerDbContext : DbContext
    {
        static CommandSchedulerDbContext()
        {
            Database.SetInitializer(new CommandSchedulerDatabaseInitializer());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSchedulerDbContext"/> class.
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public CommandSchedulerDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        /// <summary>
        /// Gets or sets the clocks.
        /// </summary>
        public virtual DbSet<Clock> Clocks { get; set; }

        /// <summary>
        /// Gets or sets the clock mappings.
        /// </summary>
        public virtual DbSet<ClockMapping> ClockMappings { get; set; }

        /// <summary>
        /// Gets or sets the scheduled commands.
        /// </summary>
        public virtual DbSet<ScheduledCommand> ScheduledCommands { get; set; }

        /// <summary>
        /// Gets or sets the command execution errors.
        /// </summary>
        public virtual DbSet<CommandExecutionError> Errors { get; set; }

        /// <summary>
        /// Gets or sets the etags.
        /// </summary>
        public virtual DbSet<ETag> ETags { get; set; }

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
            modelBuilder.Configurations.Add(new ClockEntityTypeConfiguration());
            modelBuilder.Configurations.Add(new ClockMappingsEntityTypeConfiguration());
            modelBuilder.Configurations.Add(new CommandExecutionErrorEntityTypeConfiguration());
            modelBuilder.Configurations.Add(new ScheduledCommandEntityTypeConfiguration());
            modelBuilder.Configurations.Add(new ETagEntityTypeConfiguration());
        }

        private class ScheduledCommandEntityTypeConfiguration : EntityTypeConfiguration<ScheduledCommand>
        {
            public ScheduledCommandEntityTypeConfiguration()
            {
                ToTable("ScheduledCommand", "Scheduler");

                HasKey(c => new { c.AggregateId, c.SequenceNumber });

                HasRequired(c => c.Clock);

                Property(c => c.SerializedCommand)
                    .IsRequired();

                Ignore(c => c.Result);
                Ignore(c => c.NonDurable);
            }
        }

        private class ETagEntityTypeConfiguration : EntityTypeConfiguration<ETag>
        {
            public ETagEntityTypeConfiguration()
            {
                ToTable("ETag", "Scheduler");

                HasKey(c => new { c.Id });

                Property(c => c.Scope)
                    .IsRequired();
                
                Property(c => c.ETagValue)
                    .IsRequired();

                Property(c => c.CreatedDomainTime)
                    .IsRequired();

                Property(c => c.CreatedRealTime)
                    .IsRequired();
            }
        }

        private class ClockEntityTypeConfiguration : EntityTypeConfiguration<Clock>
        {
            public ClockEntityTypeConfiguration()
            {
                ToTable("Clock", "Scheduler");

                Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(128);
            }
        }

        private class ClockMappingsEntityTypeConfiguration : EntityTypeConfiguration<ClockMapping>
        {
            public ClockMappingsEntityTypeConfiguration()
            {
                ToTable("ClockMapping", "Scheduler");

                HasRequired(m => m.Clock);

                Property(c => c.Value)
                    .HasMaxLength(128)
                    .IsRequired();
            }
        }

        private class CommandExecutionErrorEntityTypeConfiguration : EntityTypeConfiguration<CommandExecutionError>
        {
            public CommandExecutionErrorEntityTypeConfiguration()
            {
                ToTable("Error", "Scheduler");

                HasRequired(e => e.ScheduledCommand);

                Property(e => e.Error)
                    .IsRequired();
            }
        }
    }
}