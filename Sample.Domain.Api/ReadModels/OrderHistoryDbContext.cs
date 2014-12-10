using System.Data.Entity;

namespace Sample.Domain.Api.ReadModels
{
    public class OrderHistoryDbContext : DbContext
    {
        public OrderHistoryDbContext() : base("ReadModels")
        {
        }

        public virtual DbSet<OrderHistoryEntry> Orders { get; set; }
        public virtual DbSet<OrderHistoryItem> OrderItems { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderHistoryEntry>().HasKey(o => o.OrderId);
            modelBuilder.Entity<OrderHistoryItem>().HasKey(o => o.Id);
        }
    }
}