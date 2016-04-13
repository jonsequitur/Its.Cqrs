// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public partial class CheckingAccount
    {
        public class Overdrawn : Event<CheckingAccount>
        {
            public decimal Balance { get; set; }

            public override void Update(CheckingAccount aggregate)
            {
            }
        }
    }
}
