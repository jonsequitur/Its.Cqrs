using System;

namespace Sample.Domain.Ordering
{
    public interface ICreditCardInfo
    {
        string CreditCardNumber { get; set; }
        string CreditCardName { get; set; }
        string CreditCardCvv2 { get; set; }
        string CreditCardExpirationMonth { get; set; }
        string CreditCardExpirationYear { get; set; }
    }
}