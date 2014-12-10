using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public class SendOrderConfirmationEmail : Command<CustomerAccount>
    {
        public SendOrderConfirmationEmail(string orderNumber)
        {
            OrderNumber = orderNumber;
        }

        public string OrderNumber { get; set; }
    }
}