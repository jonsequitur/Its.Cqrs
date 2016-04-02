// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.Its.Domain;
using Its.Validation;
using Test.Domain.Ordering;
using Test.Domain.Ordering;

namespace Test.Domain.Ordering
{
    public class ProvideCreditCardInfo : Command<Order>, ICreditCardInfo
    {
        public string CreditCardNumber { get; set; }
        public string CreditCardName { get; set; }
        public string CreditCardCvv2 { get; set; }
        public string CreditCardExpirationMonth { get; set; }
        public string CreditCardExpirationYear { get; set; }

        [Range(.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public override IValidationRule CommandValidator
        {
            get
            {
                return CreditCardInfo.IsValid;
            }
        }
    }
}
