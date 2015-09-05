// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public class NotifyOrderCanceled : Command<CustomerAccount>
    {
        public NotifyOrderCanceled(string etag = null) : base(etag)
        {
            Debug.WriteLine("NotifyOrderCanceled ctor"); // FIX: (NotifyOrderCanceled) remove this
        }

        public string OrderNumber { get; set; }
    }
}