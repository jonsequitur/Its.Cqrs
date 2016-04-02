// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public class OpenCheckingAccount : Command<CustomerAccount>
    {
        public Guid CheckingAccountId { get; set; }
    }
}
