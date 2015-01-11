// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain
{
    public class ChangeEmailAddress : Command<CustomerAccount>
    {
        public ChangeEmailAddress(string email = null)
        {
            if (email != null)
            {
                NewEmailAddress = email;
            }
        }

        [Required]
        public EmailAddress NewEmailAddress { get; set; }

        public override IValidationRule<CustomerAccount> Validator
        {
            get
            {
                return null;
            }
        }
    }
}
