// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class RequestedNoSpam : Event<CustomerAccount>
        {
            public override void Update(CustomerAccount aggregate)
            {
                aggregate.NoSpam = true;
            }
        }
    }
}
