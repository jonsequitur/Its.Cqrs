using System;
using Microsoft.Its.Domain;

namespace Sample.Domain
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