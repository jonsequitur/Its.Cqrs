// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering
{
    public partial class Order
    {
        public class ItemAdded : Event<Order>
        {
            public ItemAdded()
            {
                Quantity = 1;
            }

            public decimal Price { get; set; }

            public string ProductName { get; set; }

            public int Quantity { get; set; }

            public override void Update(Order order)
            {
                var existingItem = order.Items
                                        .SingleOrDefault(i => i.Price == Price &&
                                                              i.ProductName == ProductName);

                if (existingItem != null)
                {
                    existingItem.Quantity += Quantity;
                }
                else
                {
                    order.Items.Add(new OrderItem
                    {
                        Price = Price,
                        Quantity = Quantity,
                        ProductName = ProductName
                    });
                }

                order.Balance += (Price*Quantity);
            }
        }
    }
}