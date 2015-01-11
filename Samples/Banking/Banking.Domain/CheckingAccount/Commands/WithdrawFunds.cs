// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

namespace Sample.Banking.Domain
{
    public class WithdrawFunds : Command<CheckingAccount>
    {
        public decimal Amount { get; set; }

        public override IValidationRule<CheckingAccount> Validator
        {
            get
            {
                var accountIsNotClosed =
                    Validate.That<CheckingAccount>(account => account.DateClosed == null)
                            .WithErrorMessage("You cannot make a withdrawal from a closed account.");

                var fundsAreAvailable = Validate.That<CheckingAccount>(account => account.Balance >= Amount)
                                                .WithErrorMessage("Insufficient funds.");

                return new ValidationPlan<CheckingAccount>
                {
                    accountIsNotClosed,
                    fundsAreAvailable.When(accountIsNotClosed)
                };
            }
        }

        public override IValidationRule CommandValidator
        {
            get
            {
                return Validate.That<WithdrawFunds>(cmd => cmd.Amount > 0)
                               .WithErrorMessage("You cannot make a withdrawal for a negative amount.");
            }
        }
    }
}