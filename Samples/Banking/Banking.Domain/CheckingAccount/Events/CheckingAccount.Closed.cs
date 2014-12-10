using Microsoft.Its.Domain;

namespace Banking.Domain
{
    public partial class CheckingAccount
    {
        public class Closed : Event<CheckingAccount>
        {
            public override void Update(CheckingAccount aggregate)
            {
                aggregate.DateClosed = Timestamp;
            }
        }
    }
}