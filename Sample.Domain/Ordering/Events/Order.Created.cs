// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class Created : Event<Order>
        {
            public string OrderNumber { get; set; }

            public string CustomerName { get; set; }

            public Guid CustomerId { get; set; }

            public override void Update(Order aggregate)
            {
                aggregate.CustomerName = CustomerName;
                aggregate.CustomerId = CustomerId;
                aggregate.OrderNumber = OrderNumber;
            }
        }
    }
}