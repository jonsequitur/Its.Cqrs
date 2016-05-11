// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;
using Its.Validation;

namespace Test.Domain.Ordering
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
