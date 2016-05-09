// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class CreditCardInfoProvided : Event<Order>, ICreditCardInfo
        {
            public override void Update(Order order)
            {
                order.PaymentInfo = new CreditCardInfo
                {
                    CreditCardNumber = CreditCardNumber,
                    CreditCardName = CreditCardName,
                    CreditCardCvv2 = CreditCardCvv2,
                    CreditCardExpirationMonth = CreditCardExpirationMonth,
                    CreditCardExpirationYear = CreditCardExpirationYear
                };
            }

            public string CreditCardNumber { get; set; }
            public string CreditCardName { get; set; }
            public string CreditCardCvv2 { get; set; }
            public string CreditCardExpirationMonth { get; set; }
            public string CreditCardExpirationYear { get; set; }
        }
    }
}