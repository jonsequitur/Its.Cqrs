// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public abstract class PaymentCommand : Command<Order>
    {
        public decimal Amount { get; set; }
    }
}
