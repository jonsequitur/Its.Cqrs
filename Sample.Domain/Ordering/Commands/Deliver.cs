using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class Deliver : Command<Order>
    {
        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.AlwaysValid;
            }
        }
    }
}