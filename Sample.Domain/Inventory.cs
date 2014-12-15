// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Sample.Domain
{
    public static class Inventory
    {
        public static Func<string, bool> IsAvailable = sku => true;
    }
}