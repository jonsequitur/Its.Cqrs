// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Its.Domain;

namespace Test.Domain.Ordering
{
    public class EmailAddress : String<EmailAddress>
    {
        private static readonly EmailAddressAttribute validator = new EmailAddressAttribute();

        public EmailAddress(string value) : base(value, StringComparison.OrdinalIgnoreCase)
        {
            if (!validator.IsValid(value))
            {
                throw new ArgumentException("Invalid email address", value);
            }
        }

        public static implicit operator EmailAddress(string email)
        {
            return new EmailAddress(email);
        }
    }
}
