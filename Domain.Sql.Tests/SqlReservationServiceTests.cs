// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReservationService")]
    public class SqlReservationServiceTests : ReservationServiceTests
    {
        static SqlReservationServiceTests()
        {
            ReservationServiceDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReservationService";
            EventStoreDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsEventStore";
        }

        protected override void Configure(Configuration configuration)
        {
            configuration.UseSqlEventStore()
                         .UseEventBus(new FakeEventBus());

            var reservationService = new SqlReservationService
            {
                CreateReservationServiceDbContext = () => new TestReservationServiceDbContext()
            };
            configuration.ReservationService = reservationService;
            configuration.RegisterForDisposal(Disposable.Create(() => onSave = null));
        }

        protected override async Task<ReservedValue> GetReservedValue(string value, string promoCode)
        {
            var reservationService = (SqlReservationService) Configuration.Current.ReservationService;
            return await reservationService.GetReservedValue(value, promoCode);
        }

        private class TestReservationServiceDbContext : ReservationServiceDbContext
        {
            public override int SaveChanges()
            {
                onSave?.Invoke();
                return base.SaveChanges();
            }

            public override Task<int> SaveChangesAsync()
            {
                onSave?.Invoke();
                return base.SaveChangesAsync();
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
    }
}