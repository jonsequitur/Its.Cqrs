using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class Created : Event<CustomerAccount>
        {
            public override void Update(CustomerAccount aggregate)
            {
            }
        }
    }
}