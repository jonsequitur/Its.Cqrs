using System;
using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public class EmailSubject : String<EmailSubject>
    {
        public EmailSubject(string value) : base(value, StringComparison.OrdinalIgnoreCase)
        {
        }

        public static implicit operator EmailSubject(string value)
        {
            return new EmailSubject(value);
        }
    }
}