using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Its.Domain;

namespace Sample.Domain
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