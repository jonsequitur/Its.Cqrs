using System;
using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering.Commands
{
    public class SpecifyShippingInfo : Command<Order>
    {
        public DateTimeOffset? DeliverBy { get; set; }
        public string RecipientName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string Country { get; set; }
    }
}