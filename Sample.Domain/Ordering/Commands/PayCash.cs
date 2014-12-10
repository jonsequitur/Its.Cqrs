using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class PayCash : PaymentCommand
    {
        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.BalanceIsAtLeast(Amount);
            }
        }
    }
}