// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Sample.Domain.Ordering
{
    public interface ICreditCardInfo
    {
        string CreditCardNumber { get; set; }
        string CreditCardName { get; set; }
        string CreditCardCvv2 { get; set; }
        string CreditCardExpirationMonth { get; set; }
        string CreditCardExpirationYear { get; set; }
    }
}
