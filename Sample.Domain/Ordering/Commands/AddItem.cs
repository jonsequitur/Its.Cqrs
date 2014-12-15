// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    /// <summary>
    ///     Adds a product item to the order.
    /// </summary>
    public class AddItem : Command<Order>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AddItem" /> class.
        /// </summary>
        public AddItem()
        {
            Quantity = 1;
        }

        /// <summary>
        ///     Gets or sets the price of the item.
        /// </summary>
        [Range(.01, double.MaxValue)]
        public decimal Price { get; set; }

        /// <summary>
        ///     Gets or sets the name of the product to be added to the order.
        /// </summary>
        [Required]
        public string ProductName { get; set; }

        /// <summary>
        ///     Gets or sets the quantity of this item to be added to the order.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

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
                };
            }
        }
    }
}