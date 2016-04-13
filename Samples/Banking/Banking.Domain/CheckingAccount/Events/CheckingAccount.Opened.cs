// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public partial class CheckingAccount
    {
        public class Opened : Event<CheckingAccount>
        {
            public Guid CustomerAccountId { get; set; }

            public override void Update(CheckingAccount aggregate)
            {
                aggregate.DateOpened = Timestamp;
            }
        }
    }
}
