using Microsoft.Its.Domain;
using Its.Validation;
using Its.Validation.Configuration;

namespace Sample.Domain
{
    public class RequestUserName : Command<CustomerAccount>
    {
        public string UserName { get; set; }

        public override IValidationRule CommandValidator
        {
            get
            {
                var isNotEmpty = Validate.That<RequestUserName>(cmd => !string.IsNullOrEmpty(cmd.UserName))
                                         .WithMessage("User name cannot be empty.");

                var isUnique = Validate.That<RequestUserName>(cmd => cmd.RequiresReserved(c => c.UserName, "UserName", ((ICommand) cmd).Principal.Identity.Name).Result)
                                       .WithErrorMessage((f, c) => string.Format("The user name {0} is taken. Please choose another.", c.UserName));

                return new ValidationPlan<RequestUserName>
                {
                    isNotEmpty,
                    isUnique.When(isNotEmpty)
                };
            }
        }
    }
}