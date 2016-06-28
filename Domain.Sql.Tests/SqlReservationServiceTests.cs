// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReservationService")]
    public class SqlReservationServiceTests : ReservationServiceTests
    {
        protected override void Configure(Configuration configuration)
        {
            configuration.UseSqlEventStore(c => c.UseConnectionString(EventStore.ConnectionString))
                         .UseSqlReservationService(c => c.UseConnectionString(ReservationService.ConnectionString))
                         .UseEventBus(new FakeEventBus());

            configuration.RegisterForDisposal(Disposable.Create(() => onSave = null));
        }

        protected override async Task<ReservedValue> GetReservedValue(string value, string promoCode)
        {
            var reservationService = (SqlReservationService) Configuration.Current.ReservationService();
            return await reservationService.GetReservedValue(value, promoCode);
        }

        [Test]
        public async Task When_simultaneous_reservations_are_placed_for_one_of_a_fixed_quantity_of_a_resource_then_different_values_are_reserved()
        {
            //arrange
            var barrier = new Barrier(2);

            var reservationService = new SqlReservationService(() => new ReservationServiceDbContextThatForcesConcurrencyDuringSave(barrier));

            // given a fixed quantity of some resource, e.g. promo codes:
            var promoCode = "promo-code-" + Any.Word();
            var reservedValue1 = "firstValue:" + Any.CamelCaseName();
            var reservedValue2 = "SecondValue:" + Any.CamelCaseName();
            await reservationService.Reserve(reservedValue1, promoCode, Any.CamelCaseName(), TimeSpan.FromDays(-1));
            await reservationService.Reserve(reservedValue2, promoCode, Any.CamelCaseName(), TimeSpan.FromDays(-1));

            //act
            var result = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: Any.FullName(),
                lease: TimeSpan.FromMinutes(2),
                confirmationToken: Any.CamelCaseName());

            var result2 = await reservationService.ReserveAny(
                scope: promoCode,
                ownerToken: Any.FullName(),
                lease: TimeSpan.FromMinutes(2),
                confirmationToken: Any.CamelCaseName());

            //assert
            result.Should().NotBe(result2);
        }
    }

    public class ReservationServiceDbContextThatForcesConcurrencyDuringSave : ReservationServiceDbContext {
        private readonly Barrier barrier;

        public ReservationServiceDbContextThatForcesConcurrencyDuringSave(Barrier barrier) : base(ReservationService.ConnectionString)
        {
            this.barrier = barrier;
        }

        public override Task<int> SaveChangesAsync()
        {
            barrier.SignalAndWait(3.Seconds());
            return base.SaveChangesAsync();
        }
    }


}