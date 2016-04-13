// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class CustomerInfoChanged : Event<Order>
        {
            public string CustomerName { get; set; }

            public Optional<string> Address { get; set; }

            public Optional<string> PostalCode { get; set; }

            public Optional<string> RegionOrCountry { get; set; }

            public Optional<string> PhoneNumber { get; set; }

            public override void Update(Order order)
            {
                order.CustomerName = CustomerName;
            }
        }
    }
}