// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlReservationServiceTests : EventStoreDbTest
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            ReservationServiceDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReservationService";

#if !DEBUG
            new ReservationServiceDbContext().Database.Delete();
#endif

            using (var db = new ReservationServiceDbContext())
            {
                new ReservationServiceDatabaseInitializer().InitializeDatabase(db);
            }

            // disable authorization checks
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [SetUp]
        public void SetUp()
        {
            Configuration.Global.ReservationService = new SqlReservationService();
            Command<CustomerAccount>.AuthorizeDefault = (account, command) => true;
        }

        [Test]
        public void When_a_command_reserves_a_unique_value_then_a_subsequent_request_by_the_same_owner_succeeds()
        {
            // arrange
            var username = Any.CamelCaseName(5);

            // act
            var account1 = new CustomerAccount();
            var principal = new Customer
            {
                Name = Any.CamelCaseName()
            };
            account1.Apply(new RequestUserName
            {
                UserName = username,
                Principal = principal
            });
            account1.ConfirmSave();

            var account2 = new CustomerAccount();
            var secondAttempt = account2.Validate(new RequestUserName
            {
                UserName = username,
                Principal = principal
            });

            // assert
            secondAttempt.ShouldBeValid();
        }

        [Test]
        public void When_a_command_reserves_a_unique_value_then_a_subsequent_request_by_a_different_owner_fails()
        {
            // arrange
            var username = Any.CamelCaseName(5);

            // act
            var account1 = new CustomerAccount();
            account1.Apply(new RequestUserName
            {
                UserName = username,
                Principal = new Customer
                {
                    Name = Any.CamelCaseName()
                }
            });
            account1.ConfirmSave();

            var account2 = new CustomerAccount();
            var secondPrincipal = new Customer
            {
                Name = Any.CamelCaseName()
            };
            var secondAttempt = account2.Validate(new RequestUserName
            {
                UserName = username,
                Principal = secondPrincipal
            });

            // assert
            secondAttempt.ShouldBeInvalid(string.Format("The user name {0} is taken. Please choose another.", username));
        }

        [Test]
        public void When_a_command_reserves_a_unique_value_but_it_expires_then_a_subsequent_request_by_a_different_actor_succeeds()
        {
            // arrange
            Clock.Now = () => DateTimeOffset.Parse("2014-1-1");
            var username = Any.CamelCaseName(5);
            using (var db = new ReservationServiceDbContext())
            {
                db.Set<ReservedValue>().Add(new ReservedValue
                {
                    OwnerToken = Any.CamelCaseName(),
                    Scope = "UserName",
                    Value = username,
                    ConfirmationToken = username,
                    Expiration = Clock.Now().Subtract(TimeSpan.FromMinutes(30))
                });
                db.SaveChanges();
            }

            // act
            var attempt = new CustomerAccount()
                .Validate(new RequestUserName
                {
                    UserName = username,
                    Principal = new Customer
                    {
                        Name = Any.CamelCaseName()
                    }
                });

            // assert
            attempt.ShouldBeValid();
            using (var db = new ReservationServiceDbContext())
            {
                db.Set<ReservedValue>().First(v => v.Value == username).Expiration.Should().Be(Clock.Now().AddMinutes(1));
            }
        }

        [Test]
        public async Task When_the_aggregate_is_saved_then_the_reservation_is_confirmed()
        {
            // arrange
            var username = Any.Email();

            var account = new CustomerAccount();
            account.Apply(new RequestUserName
            {
                UserName = username,
                Principal = new Customer(username)
            });
            var bus = new FakeEventBus();
            bus.Subscribe(new UserNameConfirmer());
            var repository = new SqlEventSourcedRepository<CustomerAccount>(bus);

            // act
            await repository.Save(account);

            // assert
            using (var db = new ReservationServiceDbContext())
            {
                db.Set<ReservedValue>()
                  .Single(v => v.Value == username && v.Scope == "UserName")
                  .Expiration
                  .Should()
                  .BeNull();
            }
        }

        [Test]
        public async Task When_a_value_is_confirmed_then_it_can_be_re_reserved_using_the_same_owner_token_because_idempotency()
        {
            // arrange
            var value = Any.FullName();
            var ownerToken = Any.Guid().ToString();
            var scope = "default-scope";
            await Configuration.Global.ReservationService.Reserve(value, scope, ownerToken);
            await Configuration.Global.ReservationService.Confirm(value, scope, ownerToken);

            // act
            var succeeded = await Configuration.Global.ReservationService.Reserve(value, scope, ownerToken);

            // assert
            succeeded.Should().BeTrue("because idempotency");
        }

        [Test]
        public async Task When_a_value_is_confirmed_then_an_attempt_using_a_different_owner_token_to_reserve_it_again_throws()
        {
            // arrange
            var username = Any.Email();
            var ownerToken = Any.Email();
            var scope = "default-scope";
            await Configuration.Global.ReservationService.Reserve(username, scope, ownerToken);
            await Configuration.Global.ReservationService.Confirm(username, scope, ownerToken);

            // act
            var succeeded = await Configuration.Global.ReservationService.Reserve(username, scope, ownerToken + "!");

            // assert
            succeeded.Should().BeFalse();
        }

        [Test]
        public async Task A_reservation_cannot_be_confirmed_using_the_wrong_owner_token()
        {
            // arrange
            var value = Any.FullName();
            var ownerToken = Any.Guid().ToString();
            var scope = "default-scope";
            await Configuration.Global.ReservationService.Reserve(value, scope, ownerToken, TimeSpan.FromMinutes(5));

            // act
            var wrongOwnerToken = Any.Guid();
            var confirmed = Configuration.Global.ReservationService.Confirm(value, scope, wrongOwnerToken.ToString()).Result;

            // assert
            confirmed.Should().BeFalse();
        }

        [Test]
        public async Task A_reservation_can_be_cancelled_using_its_owner_token()
        {
            // arrange
            var value = Any.FullName();
            var ownerToken = Any.Guid().ToString();
            var scope = "default-scope";
            await Configuration.Global.ReservationService.Reserve(value, scope, ownerToken, TimeSpan.FromMinutes(5));

            // act
            await Configuration.Global.ReservationService.Cancel(
                value: value,
                scope: scope,
                ownerToken: ownerToken);

            // assert
            // check that someone else can now reserve the same value
            var succeeded = await Configuration.Global.ReservationService.Reserve(value, scope, Any.Guid().ToString(), TimeSpan.FromMinutes(5));

            succeeded.Should().BeTrue();
        }

        [Test]
        public async Task An_attempt_to_cancel_a_reservation_without_the_correct_owner_token_fails()
        {
            // arrange
            var value = Any.FullName();
            var ownerToken = Any.Guid().ToString();
            var scope = "default-scope";
            await Configuration.Global.ReservationService.Reserve(value, scope, ownerToken, TimeSpan.FromMinutes(5));

            // act
            var wrongOwnerToken = Any.Guid().ToString();
            var cancelled = Configuration.Global.ReservationService.Cancel(value, scope, wrongOwnerToken).Result;

            // assert
            cancelled.Should().BeFalse();
        }

        [Test]
        public async Task Reservations_can_be_placed_for_one_of_a_fixed_quantity_of_a_resource()
        {
            var reservationService = new SqlReservationService();

            // given a fixed quantity of some resource, e.g. promo codes:
            var word = Any.Word();
            var ownerToken = "ownerToken-" + Any.Guid();
            var promoCode = "promo-code-" + word;
            var reservedValue = "reservedValue-" + Any.Guid();
            var confirmationToken = "userConfirmationCode-" + Any.Guid();
            await reservationService.Reserve(reservedValue, promoCode, reservedValue, TimeSpan.FromDays(-1));

            //act
            var result = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: ownerToken,
                lease: TimeSpan.FromMinutes(2), 
                confirmationToken: confirmationToken);

            result.Should().Be(reservedValue);

            await reservationService.Confirm(confirmationToken, promoCode, ownerToken);

            // assert
            using (var db = new ReservationServiceDbContext())
            {
                db.Set<ReservedValue>()
                  .Should().Contain(v => v.Value == reservedValue
                                         && v.Scope == promoCode
                                         && v.Expiration == null);
            }
        }

        [Test]
        public async Task The_value_returned_by_the_reservation_service_can_be_used_for_confirmation()
        {
            var reservationService = new SqlReservationService();

            // given a fixed quantity of some resource, e.g. promo codes:
            var ownerToken = "owner-token-" + Any.Email();
            var promoCode = "promo-code-" + Any.Word();
            var reservedValue = Any.Email();
            await reservationService.Reserve(reservedValue, 
                scope: promoCode,
                ownerToken: ownerToken, 
                lease: TimeSpan.FromDays(-1));

            //act
            var value = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: ownerToken,
                lease: TimeSpan.FromMinutes(2));

            value.Should().NotBeNull();

            await reservationService.Confirm(value, promoCode, ownerToken);

            // assert
            using (var db = new ReservationServiceDbContext())
            {
               var reservation = db.Set<ReservedValue>()
                  .Single(v => v.Value == reservedValue
                               && v.Scope == promoCode
                               && v.Expiration == null);
                reservation.ConfirmationToken.Should().Be(value);
            }
        }

        [Test]
        public async Task When_simultaneous_reservations_are_placed_for_one_of_a_fixed_quantity_of_a_resource_then_different_values_are_reserved()
        {
            //arrange
            var reservationService1 = new SqlReservationService();
            var reservationService2 = new SqlReservationService();

            // given a fixed quantity of some resource, e.g. promo codes:
            var promoCode = "promo-code-" + Any.Word();
            var reservedValue1 = "firstValue:" + Any.Guid();
            var reservedValue2 = "SecondValue:" + Any.Guid();
            await reservationService1.Reserve(reservedValue1, promoCode, Any.CamelCaseName(), TimeSpan.FromDays(-1));
            await reservationService2.Reserve(reservedValue2, promoCode, Any.CamelCaseName(), TimeSpan.FromDays(-1));

            //Both services retrieve the same entry on the first reservation attempt
            //A subsequent call will give a different entry
            var db1 = new ReservationServiceDbContext();
            var db2 = new ReservationServiceDbContext();
            reservationService1.CreateReservationServiceDbContext = () => db1;
            reservationService2.CreateReservationServiceDbContext = () => db2;

            var entry1 = db1.ReservedValues.Single(r => r.Value == reservedValue1);
            var entry_1 = db2.ReservedValues.Single(r => r.Value == reservedValue1);
            var entry2 = db2.ReservedValues.Single(r => r.Value == reservedValue2);

            var queue = new Queue<ReservedValue>(new[]
            {
                entry_1,
                entry2
            });

            reservationService1.GetValueToReserve = (reservedValues, scope, now) => Task.FromResult(entry1);
            reservationService2.GetValueToReserve = (reservedValues, scope, now) => Task.FromResult(queue.Dequeue());

            //act
            var result = await reservationService1.ReserveAny(
                scope: promoCode,
                ownerToken: Any.FullName(),
                lease: TimeSpan.FromMinutes(2), 
                confirmationToken: Any.CamelCaseName());

            var result2 = await reservationService2.ReserveAny(
                scope: promoCode,
                ownerToken: Any.FullName(),
                lease: TimeSpan.FromMinutes(2), 
                confirmationToken: Any.CamelCaseName());

            //assert
            result.Should().NotBe(result2);
        }

        [Test]
        public async Task If_a_fixed_quantity_of_resource_had_been_depleted_then_reservations_cant_be_made()
        {
            var reservationService = new SqlReservationService();

            // given a fixed quantity of some resource where the resource has been used
            var word = Any.Word();
            var ownerToken = Any.Guid().ToString();
            var promoCode = "promo-code-" + word;
            var reservedValue = Any.Guid().ToString();
            var userConfirmationCode = Any.Guid().ToString();
            await reservationService.Reserve(reservedValue, promoCode, reservedValue, TimeSpan.FromDays(-1));

            //act
            var result = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: ownerToken,
                lease: TimeSpan.FromMinutes(-2), confirmationToken: userConfirmationCode);

            result.Should().Be(reservedValue);
            await reservationService.Confirm(userConfirmationCode, promoCode, ownerToken);

            //assert
            result = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: Any.FullName(),
                lease: TimeSpan.FromMinutes(2), confirmationToken: Any.Guid().ToString());
            result.Should().BeNull();
        }

        
        [Test]
        public async Task Confirmation_token_cant_be_used_twice_by_different_owners_for_the_same_resource()
        {
            var reservationService = new SqlReservationService();

            // given a fixed quantity of some resource where the resource has been used
            var word = Any.Word();
            var ownerToken = Any.Guid().ToString();
            var promoCode = "promo-code-" + word;
            var reservedValue = Any.Guid().ToString();
            var confirmationToken = Any.Guid().ToString();
            await reservationService.Reserve(reservedValue, promoCode, reservedValue, TimeSpan.FromDays(-1));
            await reservationService.Reserve(Any.Guid().ToString(), promoCode, reservedValue, TimeSpan.FromDays(-1));

            //act
            await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: ownerToken,
                confirmationToken: confirmationToken);

            //assert
            Action reserve = () =>
            {
                var result = reservationService.ReserveAny(
                    scope: promoCode,
                    ownerToken: Any.FullName(),
                    lease: TimeSpan.FromMinutes(2), confirmationToken: confirmationToken)
                                               .Result;
            };
            reserve.ShouldThrow<DbUpdateException>();
        }

        [Test]
        public async Task When_confirmation_token_is_used_twice_for_the_same_unconfirmed_reservation_then_ReserveAny_extends_the_lease()
        {
            var reservationService = new SqlReservationService();

            // given a fixed quantity of some resource where the resource has been used
            //todo:(this needs to be done via an interface rather then just calling reserve multiple times)
            var word = Any.Word();
            var ownerToken = Any.Guid().ToString();
            var promoCode = "promo-code-" + word;
            var reservedValue = Any.Guid().ToString();
            var confirmationToken = Any.Guid().ToString();
            await reservationService.Reserve(reservedValue, promoCode, reservedValue, TimeSpan.FromDays(-1));
            await reservationService.Reserve(Any.Guid().ToString(), promoCode, reservedValue, TimeSpan.FromDays(-1));

            //act
            var firstAttempt = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: ownerToken,
                confirmationToken: confirmationToken);

            //assert
            var secondAttempt = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: ownerToken,
                lease: TimeSpan.FromMinutes(2),
                confirmationToken: confirmationToken);

            secondAttempt.Should()
                         .NotBeNull()
                         .And
                         .Be(firstAttempt);
        }

        [Test]
        public async Task When_ReserveAny_is_called_for_a_scope_that_has_no_entries_at_all_then_it_returns_false()
        {
            var reservationService = new SqlReservationService();

            var value = await reservationService.ReserveAny(Any.CamelCaseName(), Any.CamelCaseName(), TimeSpan.FromMinutes(1));

            value.Should().BeNull();
        }

        [Test]
        public async Task When_Confirm_is_called_for_a_nonexistent_reservation_then_it_returns_false()
        {
            var reservationService = new SqlReservationService();

            var value = await reservationService.Confirm(Any.CamelCaseName(), Any.CamelCaseName(), Any.CamelCaseName());

            value.Should().BeFalse();
        }
    }

    public class UserNameConfirmer : IHaveConsequencesWhen<CustomerAccount.UserNameAcquired>
    {
        public void HaveConsequences(CustomerAccount.UserNameAcquired @event)
        {
            Configuration.Global.ReservationService.Confirm(
                @event.UserName,
                "UserName",
                @event.UserName).Wait();
        }
    }
}
