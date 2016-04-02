// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Test.Domain.Ordering;

namespace Test.Domain.Ordering
{
    public interface IPaymentService
    {
        Task<PaymentId> Charge(decimal amount);
    }
}
