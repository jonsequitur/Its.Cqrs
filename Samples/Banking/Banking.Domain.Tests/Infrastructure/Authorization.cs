// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Microsoft.Its.Domain.Authorization;

namespace Test.Domain.Banking.Tests.Infrastructure
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
