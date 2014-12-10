using System;

namespace Sample.Domain.Api.ReadModels
{
    public class OrderHistoryItem
    {
        public Guid Id { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ProductName { get; set; }
    }
}