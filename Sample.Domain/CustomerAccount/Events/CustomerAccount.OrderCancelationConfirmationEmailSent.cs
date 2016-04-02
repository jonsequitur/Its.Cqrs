// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Domain.Ordering
{
    public partial class CustomerAccount
    {
        public class OrderCancelationConfirmationEmailSent : EmailSent
        {
            public string OrderNumber { get; set; }

            public override void Update(CustomerAccount aggregate)
            {
                aggregate.CommunicationsSent.Add(string.Format("Your order has canceled! (Order #{0})", OrderNumber));
            }
        }
    }
}
