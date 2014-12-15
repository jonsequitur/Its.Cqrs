// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class ChargeAccount : Command<Order>
    {
        [Required]
        public string AccountNumber { get; set; }

        public override IValidationRule<Order> Validator
        {
            get
            {
                var hasBeenShipped = Order.HasBeenShipped;

                return new ValidationPlan<Order>
                {
                    hasBeenShipped
                };
            }
        }
    }
}