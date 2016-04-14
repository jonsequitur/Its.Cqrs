// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Test.Domain.Ordering
{
    public class RemoveItem : Command<Order>
    {
        public RemoveItem()
        {
            Quantity = 1;
        }

        public decimal Price { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                return new ValidationPlan<Order>
                {
                    Order.NotFulfilled,
                    Validate.That<Order>(o => o.Items.Single(i => i.Price == Price && i.ProductName == ProductName).Quantity >= Quantity)
                };
            }
        }
    }
}
