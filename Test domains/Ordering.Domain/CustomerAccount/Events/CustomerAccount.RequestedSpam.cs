// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class CustomerAccount
    {
        public class RequestedSpam : Event<CustomerAccount>
        {
            public override void Update(CustomerAccount aggregate)
            {
                aggregate.NoSpam = false;
            }
        }
    }
}
