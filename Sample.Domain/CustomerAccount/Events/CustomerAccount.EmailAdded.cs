using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class EmailAddressChanged : Event<CustomerAccount>
        {
            public override void Update(CustomerAccount aggregate)
            {
                aggregate.EmailAddress = NewEmailAddress;
            }

            public EmailAddress NewEmailAddress { get; set; }
        }
    }
}