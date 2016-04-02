// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain;
using Its.Validation;
using Test.Domain.Ordering;

namespace Test.Domain.Ordering
{
    public class Cancel : Command<Order>
    {
        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.NotFulfilled;
            }
        }
    }
}
