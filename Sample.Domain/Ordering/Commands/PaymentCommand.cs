using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering.Commands
{
    public abstract class PaymentCommand : Command<Order>
    {
        public decimal Amount { get; set; }
    }
}