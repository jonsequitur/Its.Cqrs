using Microsoft.Its.Domain;

namespace Banking.Domain
{
    public partial class CheckingAccount
    {
        public class FundsWithdrawn : Event<CheckingAccount>
        {
            public decimal Amount { get; set; }

            public override void Update(CheckingAccount aggregate)
            {
                aggregate.Balance -= Amount;
            }
        }
    }
}