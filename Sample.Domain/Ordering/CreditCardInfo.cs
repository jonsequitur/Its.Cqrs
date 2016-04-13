// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Test.Domain.Ordering
{
    public partial class CreditCardInfo :
        ICreditCardInfo,
        IPaymentInfo
    {
        public string CreditCardNumber { get; set; }
        public string CreditCardName { get; set; }
        public string CreditCardCvv2 { get; set; }
        public string CreditCardExpirationMonth { get; set; }
        public string CreditCardExpirationYear { get; set; }

        public PaymentMethod PaymentMethod
        {
            get
            {
                return PaymentMethod.CreditCard;
            }
        }
    }
}
