// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain;

namespace Sample.Banking.Domain
{
    public partial class CustomerAccount
    {
        public class CheckingAccountOpened : Event<CustomerAccount>
        {
            public Guid CheckingAccountId { get; set; }

            public override void Update(CustomerAccount aggregate)
            {
                aggregate.CheckingAccounts.Add(CheckingAccountId);
            }
        }
    }
}
