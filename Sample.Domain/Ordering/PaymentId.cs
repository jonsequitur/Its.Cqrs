using System;
using Microsoft.Its.Domain;

namespace Sample.Domain.Ordering
{
    public class PaymentId : String<PaymentId>
    {
        public PaymentId(string value) : base(value, StringComparison.OrdinalIgnoreCase)
        {
        }
    }
}