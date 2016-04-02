// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public class PaymentId : String<PaymentId>
    {
        public PaymentId(string value) : base(value, StringComparison.OrdinalIgnoreCase)
        {
        }
    }
}
