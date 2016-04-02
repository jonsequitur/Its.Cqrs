// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;
using Test.Domain.Ordering;

namespace Test.Domain.Ordering
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
