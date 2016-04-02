// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public class NotifyOrderCanceled : Command<CustomerAccount>
    {
        public string OrderNumber { get; set; }
    }
}