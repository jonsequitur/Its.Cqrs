// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Threading;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Projections;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Ignore("Perf test only")]
    [Category("Catchups")]
    [Category("Performance")]
    [TestFixture]
    public class ReadModelCatchupPerfTests : EventStoreDbTest
    {
        private static long startAtEventId;

        [TestFixtureSetUp]
        public void Init()
        {
            var howMany = 500;

            startAtEventId = Events.Write(howMany, _ => Events.Any());

            startAtEventId = startAtEventId - howMany;
        }

        public override void SetUp()
        {
            base.SetUp();
            GC.Collect();
            Thread.Sleep(1000);
        }

        [Test]
        public void Read_speed_for_one_specific_event_type()
        {
            var eventsRead = 0;

            var projector1 = Projector.Create<Order.ItemAdded>(e => { eventsRead++; }).Named(MethodBase.GetCurrentMethod().Name + ":projector1");

            using (var catchup = new ReadModelCatchup(projector1) { StartAtEventId = startAtEventId })
            {
                catchup.Run();
            }

            Console.WriteLine(new { eventsRead });
        }

        [Test]
        public void Read_speed_for_IEvent()
        {
            var eventsRead = 0;

            var projector1 = Projector.Create<IEvent>(e => { eventsRead++; }).Named(MethodBase.GetCurrentMethod().Name + ":projector1");

            using (var catchup = new ReadModelCatchup(projector1) { StartAtEventId = startAtEventId })
            {
                catchup.Run();
            }

            Console.WriteLine(new { eventsRead });
        }

        [Test]
        public void Read_speed_for_two_specific_events()
        {
            var eventsRead = 0;

            var projector1 = Projector.Create<Order.ItemAdded>(e => { eventsRead++; }).Named(MethodBase.GetCurrentMethod().Name + ":projector1");
            var projector2 = Projector.Create<CustomerAccount.RequestedNoSpam>(e => { eventsRead++; }).Named(MethodBase.GetCurrentMethod().Name + ":projector2");

            using (var catchup = new ReadModelCatchup(projector1, projector2) { StartAtEventId = startAtEventId })
            {
                catchup.Run();
            }

            Console.WriteLine(new { eventsRead });
        }

        [Test]
        public void Projection_write_speed_with_unit_of_work()
        {
            var eventsRead = 0;

            var projector1 = Projector.Create<IEvent>(e =>
            {
                using (var update = this.Update())
                {
                    var db = update.Resource<ReadModelDbContext>();

                    db.Set<ProductInventory>().Add(new ProductInventory
                    {
                        ProductName = Guid.NewGuid().ToString(),
                        QuantityInStock = Any.Int(1, 5),
                        QuantityReserved = 0
                    });

                    db.SaveChanges();

                    eventsRead++;
                }
            }).Named(MethodBase.GetCurrentMethod().Name + ":projector1");

            using (var catchup = new ReadModelCatchup(projector1) { StartAtEventId = startAtEventId })
            {
                catchup.Run();
            }

            Console.WriteLine(new { eventsRead });
        }

        [Test]
        public void Projection_write_speed_without_unit_of_work()
        {
            var eventsRead = 0;

            var projector1 = Projector.Create<IEvent>(e =>
            {
                using (var db = new ReadModelDbContext())
                {
                    db.Set<ProductInventory>().Add(new ProductInventory
                    {
                        ProductName = Guid.NewGuid().ToString(),
                        QuantityInStock = Any.Int(1, 5),
                        QuantityReserved = 0
                    });

                    db.SaveChanges();

                    eventsRead++;
                }
            }).Named(MethodBase.GetCurrentMethod().Name + ":projector1");

            using (var catchup = new ReadModelCatchup(projector1) { StartAtEventId = startAtEventId })
            {
                catchup.Run();
            }

            Console.WriteLine(new { eventsRead });

            // TODO: (Write_speed_without_unit_of_work) write test
            Assert.Fail("Test not written yet.");
        }
    }
}
