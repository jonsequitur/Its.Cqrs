// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Authorization;

namespace Test.Domain.Ordering
{
    public static class AuthorizationPolicy
    {
        static AuthorizationPolicy()
        {
            AuthorizationFor<Customer>.ToApply<AddItem>.ToA<Order>
                                      .Requires((customer, addItem, order) =>
                                                customer.IsAuthenticated && customer.Id == order.CustomerId);
        }
    }
}
