// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sample.Domain.Ordering
{
    public class ShippingMethod : IDeliveryMethod
    {
        public string Carrier { get; set; }
        public string ServiceMethod { get; set; }
        public decimal Price { get; set; }
    }
}