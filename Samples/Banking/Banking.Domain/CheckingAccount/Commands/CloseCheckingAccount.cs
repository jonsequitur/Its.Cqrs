// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

namespace Test.Domain.Banking
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
}
