// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class PaymentConfirmed : Event<Order>
        {
            public PaymentId PaymentId { get; set; }

            public override void Update(Order aggregate)
            {
            }
        }
    }
}