// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering.Commands
{
    public abstract class PaymentCommand : Command<Order>
    {
        public decimal Amount { get; set; }
    }
}
