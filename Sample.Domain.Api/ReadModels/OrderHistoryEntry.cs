using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample.Domain.Api.ReadModels
{
    public class OrderHistoryEntry
    {
        public DateTime? CancelledOn { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime? DeliveredOn { get; set; }
        public DateTime? ErrorOn { get; set; }
        public ICollection<OrderHistoryItem> Items { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime? PlacedOn { get; set; }
        public DateTime? ShippedOn { get; set; }
        public decimal TotalPrice { get; set; }
    }
}