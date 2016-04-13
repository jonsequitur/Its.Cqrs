// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class FulfillmentMethodSelected : Event<Order>
        {
            public FulfillmentMethod FulfillmentMethod { get; set; }

            public override void Update(Order order)
            {
                order.FulfillmentMethod = FulfillmentMethod;
            }
        }
    }
}