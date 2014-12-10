using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.Its.Domain.Authorization;

namespace Banking.Domain.Tests.Infrastructure
{
    public static class Authorization
    {
        static Authorization()
        {
            AuthorizationFor<TestPrincipal>.ToApplyAnyCommand.ToA<CheckingAccount>
                                           .Requires((principal, acct) => true);
            
            AuthorizationFor<TestPrincipal>.ToApplyAnyCommand.ToA<CheckingAccount>
                                           .Requires((principal, acct) => true);
        }

        public static void AuthorizeAllCommands()
        {
            Thread.CurrentPrincipal = new TestPrincipal();
        }

        public class TestPrincipal : IPrincipal
        {
            public bool IsInRole(string role)
            {
                return true;
            }

            public IIdentity Identity { get; private set; }
        }
    }
}