using System.Data.Entity;

namespace Microsoft.Its.Domain.Sql
{
    public class EventStoreDbContext_v0_8 : EventStoreDbContext
    {
        static EventStoreDbContext_v0_8()
        {
            Database.SetInitializer<EventStoreDbContext_v0_8>(null);
        }

        public EventStoreDbContext_v0_8() : base(NameOrConnectionString)
        {
        }

        public EventStoreDbContext_v0_8(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StorableEvent>()
                        .Ignore(e => e.ETag);
        }

        public new static string NameOrConnectionString { get; set; }
    }
}