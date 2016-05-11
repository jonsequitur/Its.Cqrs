// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public class SendMarketingEmailOn : Command<CustomerAccount>
    {
        public DateTimeOffset Date { get; set; }

        public SendMarketingEmailOn(DateTimeOffset date)
        {
            Date = date;
        }
    }
}
