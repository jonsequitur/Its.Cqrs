using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering.Commands
{
    public class ConfirmPayment : Command<Order>
    {
        public PaymentId PaymentId { get; set; }
    }
}