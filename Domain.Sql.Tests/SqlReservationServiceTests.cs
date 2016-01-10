// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlReservationServiceTests : ReservationServiceTests
    {
        static SqlReservationServiceTests()
        {
            ReservationServiceDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReservationService";
            EventStoreDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsEventStore";
            Database.SetInitializer(new ReservationServiceDatabaseInitializer());
            Database.SetInitializer(new EventStoreDatabaseInitializer<EventStoreDbContext>());

#if !DEBUG
            new ReservationServiceDbContext().Database.Delete();
#endif

            using (var db = new ReservationServiceDbContext())
            {
                new ReservationServiceDatabaseInitializer().InitializeDatabase(db);
            }
        }

        protected override void Configure(Configuration configuration, Action onSave = null)
        {
            configuration.UseSqlReservationService()
                .UseSqlEventStore()
                .UseEventBus(new FakeEventBus());
        }

        protected override IEventSourcedRepository<TAggregate> CreateRepository<TAggregate>(
            Action onSave = null)
        {
            var repository = Configuration.Current.Repository<TAggregate>() as SqlEventSourcedRepository<TAggregate>;

            if (onSave != null)
            {
                repository.GetEventStoreContext = () =>
                {
                    onSave();
                    return new EventStoreDbContext();
                };
            }

            return repository;
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
