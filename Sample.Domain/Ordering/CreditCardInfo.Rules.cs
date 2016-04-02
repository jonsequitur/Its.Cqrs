// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;
using Its.Validation.Configuration;

namespace Test.Domain.Ordering
{
    public partial class CreditCardInfo
    {
        public static readonly IValidationRule<ICreditCardInfo> IsValid = new ValidationPlan<ICreditCardInfo>
        {
            Validate.That<ICreditCardInfo>(i => !string.IsNullOrWhiteSpace(i.CreditCardCvv2))
                    .WithErrorMessage("CreditCardCvv2 is required"),
            Validate.That<ICreditCardInfo>(i => !string.IsNullOrWhiteSpace(i.CreditCardExpirationMonth))
                    .WithErrorMessage("CreditCardExpirationMonth is required"),
            Validate.That<ICreditCardInfo>(i => !string.IsNullOrWhiteSpace(i.CreditCardExpirationYear))
                    .WithErrorMessage("CreditCardExpirationYear is required"),
            Validate.That<ICreditCardInfo>(i => !string.IsNullOrWhiteSpace(i.CreditCardName))
                    .WithErrorMessage("CreditCardName is required"),
            Validate.That<ICreditCardInfo>(i => !string.IsNullOrWhiteSpace(i.CreditCardNumber))
                    .WithErrorMessage("CreditCardNumber is required"),
        };
    }
}
