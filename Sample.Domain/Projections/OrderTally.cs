// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Log.Instrumentation;

namespace Test.Domain.Ordering.Projections
{
    public class OrderTally
    {
        static OrderTally()
        {
            Formatter<OrderTally>.RegisterForAllMembers();
        }

        public string Status { get; set; }

        public int Count { get; set; }

        public enum OrderStatus
        {
            Pending,
            Canceled,
            Delivered,
            Misdelivered,
            Fulfilled
        }
    }
}
