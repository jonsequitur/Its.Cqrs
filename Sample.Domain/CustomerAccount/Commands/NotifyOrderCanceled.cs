using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public class NotifyOrderCanceled : Command<CustomerAccount>
    {
        public string OrderNumber { get; set; }
    }
}