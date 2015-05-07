// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;
using Microsoft.Its.Domain.Sql;

namespace Sample.Domain.Api.ReadModels
{
    [DbConfigurationType(typeof(SqlClientDbConfiguration))]
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
