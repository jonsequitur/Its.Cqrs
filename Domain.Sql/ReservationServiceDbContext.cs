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
        public ReservationServiceDbContext() : this(NameOrConnectionString)
        {
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