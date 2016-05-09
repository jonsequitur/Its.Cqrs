// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class Placed : Event<Order>
        {
            public Placed(string orderNumber = null)
            {
                OrderNumber = orderNumber ?? Guid.NewGuid().ToString().Substring(1, 10);
            }

            public string OrderNumber { get; private set; }

            public IEnumerable<OrderItem> Items { get; private set; }

            public Guid CustomerId { get; set; }

            public decimal TotalPrice { get; private set; }

            public override void Update(Order aggregate)
            {
                Items = aggregate.Items.ToArray();
                TotalPrice = aggregate.Balance;
            }
        }
    }
}