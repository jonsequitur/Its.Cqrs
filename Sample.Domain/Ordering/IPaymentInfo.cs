// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Domain.Ordering
{
    public interface IPaymentInfo
    {
        PaymentMethod PaymentMethod { get; }
    }
}
