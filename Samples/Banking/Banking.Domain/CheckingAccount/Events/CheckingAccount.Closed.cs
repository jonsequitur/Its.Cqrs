// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public partial class CheckingAccount
    {
        public class Closed : Event<CheckingAccount>
        {
            public override void Update(CheckingAccount aggregate)
            {
                aggregate.DateClosed = Timestamp;
            }
        }
    }
}
