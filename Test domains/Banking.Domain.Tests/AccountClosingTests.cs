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
    public class AccountClosingTests
    {
        private CheckingAccount account;

        [SetUp]
        public void SetUp()
        {
            account = new CheckingAccount(Guid.NewGuid(), new[]
            {
                new CheckingAccount.Opened
                {
                    CustomerAccountId = Guid.NewGuid()
                }
            });

            Authorization.AuthorizeAllCommands();
        }

        [Test]
        public void A_checking_account_with_a_positive_balance_cannot_be_closed()
        {
            account.Apply(new DepositFunds
            {
                Amount = Any.Decimal(20, 200)
            });

            Action close = () => account.Apply(new CloseCheckingAccount());

            close.ShouldThrow<CommandValidationException>()
                 .And
                 .Message.Should().Contain("The account cannot be closed until it has a zero balance.");
        }

        [Test]
        public void A_checking_account_with_a_negative_balance_cannot_be_closed()
        {
            account.Apply(new DepositFunds
            {
                Amount = Any.Decimal(20, 200)
            });

            account.Apply(new WithdrawFunds
            {
                Amount = 1
            });

            Action close = () => account.Apply(new CloseCheckingAccount());

            close.ShouldThrow<CommandValidationException>()
                 .And
                 .Message.Should().Contain("The account cannot be closed until it has a zero balance.");
        }
    }
}