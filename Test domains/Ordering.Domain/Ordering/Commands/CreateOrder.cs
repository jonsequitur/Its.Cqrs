// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;
using Newtonsoft.Json;

namespace Test.Domain.Ordering
{
    [DebuggerStepThrough]
    public class CreateOrder : ConstructorCommand<Order>, ISpecifySchedulingBehavior
    {
        [JsonConstructor]
        public CreateOrder(Guid aggregateId, string customerName, string etag = null) : base(aggregateId, etag)
        {
            CustomerName = customerName;
            CustomerId = Guid.NewGuid();
        }

        public CreateOrder(string customerName, string etag = null) : this(Guid.NewGuid(), customerName, etag)
        {
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