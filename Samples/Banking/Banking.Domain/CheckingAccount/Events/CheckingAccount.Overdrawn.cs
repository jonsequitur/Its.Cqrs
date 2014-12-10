using Microsoft.Its.Domain;

namespace Banking.Domain
{
    public partial class CheckingAccount
    {
        public class Overdrawn : Event<CheckingAccount>
        {
            public decimal Balance { get; set; }

            public override void Update(CheckingAccount aggregate)
            {
            }
        }
    }
}