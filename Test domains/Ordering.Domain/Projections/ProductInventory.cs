// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Log.Instrumentation;

namespace Test.Domain.Ordering.Projections
{
    public class ProductInventory
    {
        static ProductInventory()
        {
            Formatter<ProductInventory>.RegisterForAllMembers();
        }

        public string ProductName { get; set; }
        public int QuantityInStock { get; set; }
        public int QuantityReserved { get; set; }
    }
}
