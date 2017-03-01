// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public partial class CheckingAccount
    {
        public class CloseCheckingAccount : Command<CheckingAccount>
        {
            public override IValidationRule<CheckingAccount> Validator
            {
                get
                {
                    return Validate.That<CheckingAccount>(account => account.Balance == 0)
                        .WithErrorMessage("The account cannot be closed until it has a zero balance.");
                }
            }
        }

        public class CloseCheckingAccountCommandHandler : ICommandHandler<CheckingAccount, CloseCheckingAccount>
        {

            public Task EnactCommand(CheckingAccount target, CloseCheckingAccount command)
            {
                target.RecordEvent(new CheckingAccount.Closed());
                return Task.FromResult(true);
            }

            public Task HandleScheduledCommandException(CheckingAccount target, CommandFailed<CloseCheckingAccount> command)
            {
                return Task.FromResult(true);
            }
        }
    }
}
