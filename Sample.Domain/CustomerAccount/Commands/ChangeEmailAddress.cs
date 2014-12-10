using System.ComponentModel.DataAnnotations;
using Microsoft.Its.Domain;
using Its.Validation;

namespace Sample.Domain
{
    public class ChangeEmailAddress : Command<CustomerAccount>
    {
        public ChangeEmailAddress(string email = null)
        {
            if (email != null)
            {
                NewEmailAddress = email;
            }
        }

        [Required]
        public EmailAddress NewEmailAddress { get; set; }

        public override IValidationRule<CustomerAccount> Validator
        {
            get
            {
                return null;
            }
        }
    }
}