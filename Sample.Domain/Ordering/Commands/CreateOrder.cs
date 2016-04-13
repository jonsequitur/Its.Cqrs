// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Test.Domain.Ordering
{
    [DebuggerStepThrough]
    public class CreateOrder : ConstructorCommand<Order>, ISpecifySchedulingBehavior
    {
        public CreateOrder(string customerName, string etag = null) : base(etag)
        {
            CustomerName = customerName;
            CustomerId = Guid.NewGuid();
        }

        public string CustomerName { get; set; }

        public Guid CustomerId { get; set; }

        public override IValidationRule CommandValidator
        {
            get
            {
                return Validate.That<CreateOrder>(cmd => !string.IsNullOrWhiteSpace(cmd.CustomerName))
                               .WithErrorMessage("You must provide a customer name");
            }
        }

        public bool CanBeDeliveredDuringScheduling { get; set; } = true;

        public bool RequiresDurableScheduling { get; set; } = true;
    }
}
