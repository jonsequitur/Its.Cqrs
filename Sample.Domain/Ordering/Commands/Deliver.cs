// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain.Ordering.Commands
{
    public class Deliver : Command<Order>
    {
        public override IValidationRule<Order> Validator
        {
            get
            {
                return Order.AlwaysValid;
            }
        }
    }
}
