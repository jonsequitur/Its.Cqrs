using System;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class ChargeCreditCardOn : ChargeCreditCard
    {
        public DateTimeOffset ChargeDate { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                return null;
            }
        }
    }
}