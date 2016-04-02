// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering
{
    public partial class Order
    {
        public class Paid : Event<Order>
        {
            public decimal Amount { get; private set; }

            public Paid(decimal amount)
            {
                if (amount <= 0)
                {
                    throw new ArgumentException("Amount must be at least 0.");
                }
                Amount = amount;
            }

            public override void Update(Order order)
            {
                order.Balance -= Amount;
            }
        }
    }
}