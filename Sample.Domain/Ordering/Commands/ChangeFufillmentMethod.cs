using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering
{
    public class ChangeFufillmentMethod : Command<Order>
    {
        public FulfillmentMethod FulfillmentMethod { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.NotFulfilled;
            }
        }
    }
}