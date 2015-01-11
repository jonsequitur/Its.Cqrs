// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering.Commands
{
    public class SpecifyShippingInfo : Command<Order>
    {
        public DateTimeOffset? DeliverBy { get; set; }
        public string RecipientName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string Country { get; set; }
    }
}
