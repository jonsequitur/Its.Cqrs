using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

namespace Banking.Domain
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