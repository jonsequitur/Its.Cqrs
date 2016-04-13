// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class ItemRemoved : Event<Order>
        {
            public decimal Price { get; set; }

            public string ProductName { get; set; }

            public int Quantity { get; set; }

            public override void Update(Order order)
            {
                order.Items
                     .Single(i => i.Price == Price &&
                                  i.ProductName == ProductName)
                     .Quantity -= Quantity;
            }
        }
    }
}