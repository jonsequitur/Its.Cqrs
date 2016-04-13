// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public partial class Order
    {
        public class ShippingMethodSelected : Event<Order>
        {
            public string Address { get; set; }
            public string City { get; set; }
            public string StateOrProvince { get; set; }
            public string Country { get; set; }
            public DateTimeOffset? DeliverBy { get; set; }
            public string RecipientName { get; set; }
            public string ServiceMethod { get; set; }
            public decimal Price { get; set; }
            public string Carrier { get; set; }

            public override void Update(Order aggregate)
            {
                aggregate.DeliveryMethod = new ShippingMethod
                {
                    Carrier = Carrier,
                    Price = Price,
                    ServiceMethod = ServiceMethod
                };

                aggregate.MustBeDeliveredBy = DeliverBy;
                aggregate.Address = Address;
                aggregate.City = City;
                aggregate.StateOrProvince = StateOrProvince;
                aggregate.Country = Country;
                aggregate.RecipientName = RecipientName;
            }
        }
    }
}