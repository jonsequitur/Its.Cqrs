using System;
using System.Linq;
using Microsoft.Its.Domain;

namespace Sample.Banking.Domain
{
    public class OpenCheckingAccount : Command<CustomerAccount>
    {
        public Guid CheckingAccountId { get; set; }
    }
}