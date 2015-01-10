// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain
{
    public class SentMarketingEmail:Command<CustomerAccount>
    {
        public override IValidationRule<CustomerAccount> Validator
        {
            get
            {
                return Validate.That<CustomerAccount>(account => account.EmailAddress != null);
            }
        }
    }
}
