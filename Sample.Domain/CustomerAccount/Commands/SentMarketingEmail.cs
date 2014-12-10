using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain
{
    public class SentMarketingEmail:Command<CustomerAccount>
    {
        public override IValidationRule<CustomerAccount> Validator
        {
            get
            {
                return Validate.That<CustomerAccount>(account => account.EmailAddress != null);
            }
        }
    }
}