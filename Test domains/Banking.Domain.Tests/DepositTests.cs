// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Domain;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Banking.Tests.Infrastructure;

namespace Test.Domain.Banking.Tests
{
    [TestFixture]
    public class DepositTests
    {
        private CheckingAccount account;

        [SetUp]
        public void SetUp()
        {
            account = new CheckingAccount(Guid.NewGuid(), new IEvent[]
            {
                new CheckingAccount.Opened
                {
                    CustomerAccountId = Guid.NewGuid()
                },
                new CheckingAccount.FundsDeposited
                {
                    Amount = Any.PositiveInt(100)
                }
            });

            Authorization.AuthorizeAllCommands();
        }

        [Test]
        public void When_a_deposit_is_made_then_the_balance_reflects_it()
        {
            var startingBalance = account.Balance;
            var depositAmount = Any.Decimal(1, 2000);
            account.Apply(new DepositFunds
            {
                Amount = depositAmount
            });

            account.Balance.Should().Be(depositAmount + startingBalance);
        }

        [Test]
        public void A_deposit_cannot_be_made_for_a_negative_amount()
        {
            Action makeDeposit = () => account.Apply(new DepositFunds
            {
                Amount = Any.Decimal(-2000, -1)
            });

            makeDeposit.ShouldThrow<CommandValidationException>()
                       .And
                       .Message.Should().Contain("You cannot make a deposit for a negative amount.");
        }

        [Test]
        public void A_deposit_cannot_be_made_for_a_closed_account()
        {
            account
                .Apply(new WithdrawFunds
                {
                    Amount = account.Balance
                })
                .Apply(new CloseCheckingAccount());

            Action deposit = () => account.Apply(new DepositFunds
            {
                Amount = Any.Decimal(1, 100)
            });

            deposit.ShouldThrow<CommandValidationException>()
                   .And
                   .Message.Should().Contain("You cannot make a deposit into a closed account.");
        }
    }
}
