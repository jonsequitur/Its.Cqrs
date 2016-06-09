// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Test.Domain.Ordering
{
    public class Ship : Command<Order>
    {
        public string ShipmentId { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                var productIsInStock = Validate.That<OrderItem>(item => Inventory.IsAvailable(item.ProductName))
                                               .WithErrorMessage((e, item) => $"Product '{item.ProductName}' is out of stock.");

                return new ValidationPlan<Order>
                {
                    Order.NotCancelled,
                    Order.NotShipped,
                    Order.NotFulfilled,
                    Validate.That<Order>(o => o.Items.Every(productIsInStock))
                };
            }
        }
    }
}
