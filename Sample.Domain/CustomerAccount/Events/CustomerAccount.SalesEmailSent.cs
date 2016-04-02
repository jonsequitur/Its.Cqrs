// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Domain.Ordering
{
    public partial class CustomerAccount
    {
        public class MarketingEmailSent : EmailSent
        {
            public override void Update(CustomerAccount aggregate)
            {
                aggregate.CommunicationsSent.Add(EmailSubject);
            }

            public EmailSubject EmailSubject { get; set; }
        }
    }
}
