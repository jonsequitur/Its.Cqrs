// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;

namespace Test.Domain.Ordering
{
    public class PayCash : PaymentCommand
    {
        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.BalanceIsAtLeast(Amount);
            }
        }
    }
}
