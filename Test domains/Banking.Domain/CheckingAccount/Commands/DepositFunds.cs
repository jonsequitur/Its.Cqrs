// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain;

namespace Test.Domain.Banking
{
    public class DepositFunds : Command<CheckingAccount>
    {
        public decimal Amount { get; set; }

        /// <summary>
        ///     Gets a validator to check the state of the command in and of itself, as distinct from an aggregate.
        /// </summary>
        /// <remarks>
        ///     By default, this returns a <see cref="T:Microsoft.Its.Validation.ValidationPlan`1" /> where TCommand is the
        ///     command's actual type, with rules built up from any System.ComponentModel.DataAnnotations attributes applied to its
        ///     properties.
        /// </remarks>
        public override IValidationRule CommandValidator
        {
            get
            {
                return Validate.That<DepositFunds>(cmd => cmd.Amount > 0)
                    .WithErrorMessage("You cannot make a deposit for a negative amount.");
            }
        }

        /// <summary>
        ///     Gets a validator that can be used to check the valididty of the command against the state of the aggregate before
        ///     it is applied.
        /// </summary>
        public override IValidationRule<CheckingAccount> Validator
        {
            get
            {
                return Validate.That<CheckingAccount>(account => account.DateClosed == null)
                    .WithErrorMessage("You cannot make a deposit into a closed account.");
            }
        }
    }

    public partial class CheckingAccount
    {
        public class DepositFundsCommandHandler : ICommandHandler<CheckingAccount, DepositFunds>
        {
            public Task EnactCommand(CheckingAccount target, DepositFunds command)
            {
                target.RecordEvent(new FundsDeposited
                {
                    Amount = command.Amount
                });
                return Task.FromResult(true);
            }

            public Task HandleScheduledCommandException(CheckingAccount target, CommandFailed<DepositFunds> command)
            {
                return Task.FromResult(true);
            }
        }
    }
}