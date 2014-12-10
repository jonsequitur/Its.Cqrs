using System;
using System.Linq;
using Microsoft.Its.Domain;

namespace Banking.Domain
{
    public partial class CustomerAccount
    {
        public class CheckingAccountOpened : Event<CustomerAccount>
        {
            public Guid CheckingAccountId { get; set; }

            public override void Update(CustomerAccount aggregate)
            {
                aggregate.CheckingAccounts.Add(CheckingAccountId);
            }
        }
    }
}