// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain.Ordering.Commands
{
    /// <summary>
    /// Places the order.
    /// </summary>
    public class Place : Command<Order>
    {
        /// <summary>
        ///     Gets a validator that can be used to check the valididty of the command before it is applied.
        /// </summary>
        public override IValidationRule<Order> Validator
        {
            get
            {
                return new ValidationPlan<Order>
                {
                    Order.NotFulfilled,
                    Validate.That<Order>(o => o.Items.Count > 0).WithErrorMessage("Order contains no items"),
                    Order.PaymentInfoIsProvided,
                    Order.DeliveryInfoIsProvided,
                    Order.FulfillmentInfoIsProvided
                };
            }
        }
    }
}
