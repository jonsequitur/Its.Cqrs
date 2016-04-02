// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class ReservationTests
    {
        [SetUp]
        public void SetUp()
        {
            Configuration.Current.ReservationService = null;
            Command<CustomerAccount>.AuthorizeDefault = (account, command) => true;
        }

        [Test]
        public void Reserving_a_unique_value_can_happen_during_command_validation()
        {
            // arrange
            var name = Any.CamelCaseName();
            var firstCall = true;
            Configuration.Current.ReservationService = new FakeReservationService((value, scope, actor) =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    return true;
                }
                return false;
            });

            // act
            var account1 = new CustomerAccount();
            account1.Apply(new RequestUserName
            {
                UserName = name
            });
            account1.ConfirmSave();

            var account2 = new CustomerAccount();
            var secondAttempt = account2.Validate(new RequestUserName
            {
                UserName = name
            });

            // assert
            secondAttempt.ShouldBeInvalid(string.Format("The user name {0} is taken. Please choose another.", name));
        }

        private class FakeReservationService : IReservationService
        {
            private readonly Func<string, string, string, bool> request;

            public FakeReservationService(Func<string, string, string, bool> request)
            {
                this.request = request;
            }

            public Task<bool> Reserve(string value, string scope, string ownerToken, TimeSpan? lease = null)
            {
                return Task.Run(() => request(value, scope, ownerToken));
            }

            public Task<bool> Confirm(string value, string scope, string ownerToken)
            {
                return Task.Run(() => false);
            }

            public Task<bool> Cancel(string value, string scope, string ownerToken)
            {
                return Task.Run(() => false);
            }

            public Task<string> ReserveAny(string scope, string ownerToken, TimeSpan? lease = null, string confirmationToken = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}
