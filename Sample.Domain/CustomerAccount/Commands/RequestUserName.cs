// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

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
                                         .WithErrorMessage("User name cannot be empty.");

                var isUnique = Validate.That<RequestUserName>(
                    cmd =>
                        cmd.RequiresReserved(c => c.UserName,
                                             "UserName",
                                             cmd.Principal
                                                .Identity
                                                .Name).Result)
                                       .WithErrorMessage(
                                           (f, c) => string.Format("The user name {0} is taken. Please choose another.", c.UserName));

                return new ValidationPlan<RequestUserName>
                       {
                           isNotEmpty,
                           isUnique.When(isNotEmpty)
                       };
            }
        }

        public override bool RequiresDurableScheduling
        {
            get
            {
                return false;
            }
        }
    }
}