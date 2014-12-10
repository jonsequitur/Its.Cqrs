using System;
using Microsoft.Its.Domain;

namespace Banking.Domain
{
    public partial class CheckingAccount
    {
        public class Opened : Event<CheckingAccount>
        {
            public Guid CustomerAccountId { get; set; }

            public override void Update(CheckingAccount aggregate)
            {
                aggregate.DateOpened = Timestamp;
            }
        }
    }
}