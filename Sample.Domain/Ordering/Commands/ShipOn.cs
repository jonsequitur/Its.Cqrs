using System;
using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain.Ordering.Commands
{
    public class ShipOn : Command<Order>
    {
        public ShipOn(DateTimeOffset shipDate)
        {
            ShipDate = shipDate;
        }

        public DateTimeOffset ShipDate { get; set; }

        public string ShipmentId { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                var mustBeDeliveredByDueDate =
                    Validate.That<Order>(o => o.MustBeDeliveredBy > ShipDate)
                            .When(o => o.MustBeDeliveredBy != null)
                            .WithErrorMessage("The delivery date is too late.");

                return new ValidationPlan<Order>
                {
                    mustBeDeliveredByDueDate,
                    Order.NotCancelled,
                    Order.NotFulfilled
                };
            }
        }
    }
}