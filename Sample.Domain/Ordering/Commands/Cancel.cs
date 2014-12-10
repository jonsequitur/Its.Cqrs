using System;
using System.Linq;
using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class Cancel : Command<Order>
    {
        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.NotFulfilled;
            }
        }
    }
}