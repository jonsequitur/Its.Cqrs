using Microsoft.Its.Domain;

namespace Sample.Domain
{
    public partial class CustomerAccount
    {
        public class UserNameAcquired : Event<CustomerAccount>
        {
            public string UserName { get; set; }

            public override void Update(CustomerAccount aggregate)
            {
                aggregate.UserName = UserName;
            }
        }
    }
}