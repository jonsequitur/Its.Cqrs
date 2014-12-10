using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class RequestedNoSpam : Event<CustomerAccount>
        {
            public override void Update(CustomerAccount aggregate)
            {
                aggregate.NoSpam = true;
            }
        }
    }
}