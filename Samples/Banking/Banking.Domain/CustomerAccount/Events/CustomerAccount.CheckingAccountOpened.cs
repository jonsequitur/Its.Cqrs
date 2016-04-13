// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public partial class CustomerAccount
    {
        public class CheckingAccountOpened : Event<Banking.CustomerAccount>
        {
            public Guid CheckingAccountId { get; set; }

            public override void Update(Banking.CustomerAccount aggregate)
            {
                aggregate.CheckingAccounts.Add(CheckingAccountId);
            }
        }
    }
}
