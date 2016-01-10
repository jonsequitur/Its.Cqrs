// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Its.Configuration;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class EventStoreSeedingTests
    {
        private const string EventStoreConnectionString =
            @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsEventStoreSeedingTests";

        [SetUp]
        public void SetUp()
        {
            if (Database.Exists(EventStoreConnectionString))
            {
                Database.Delete(EventStoreConnectionString);
            }
            Database.SetInitializer(new EventStoreDatabaseInitializer<EventStoreDbContext>());
            EventStoreDbContext.NameOrConnectionString = EventStoreConnectionString;
        }

        [Test]
        public void When_the_EventStoreDbContext_seeds_it_calls_the_specified_seed_action_if_it_has_been_set()
        {
            var numberOfEventsToSeed = Any.PositiveInt(100);
            var methodName = MethodBase.GetCurrentMethod().Name;

            var eventStoreInitializer1 = new EventStoreDatabaseInitializer<EventStoreDbContext>();
            eventStoreInitializer1.OnSeed = ctx =>
            {
                var aggregateId = Guid.NewGuid();
                Enumerable.Range(20, numberOfEventsToSeed)
                          .ForEach(i => ctx.Events.Add(new StorableEvent
                          {
                              AggregateId = aggregateId,
                              SequenceNumber = i,
                              Body = "test event!",
                              StreamName = GetType().Name,
                              Type = methodName
                          }));
            };

            using (var eventStore = new EventStoreDbContext())
            {
                eventStoreInitializer1.InitializeDatabase(eventStore);
                eventStore.Events.Count().Should().Be(numberOfEventsToSeed);
            }
        }

        [Test]
        public void SeedFromFile_can_be_used_to_seed_the_event_store_from_config()
        {
            var eventStoreInitializer1 = new EventStoreDatabaseInitializer<EventStoreDbContext>();
            eventStoreInitializer1.OnSeed = ctx =>
            {
                var file = Settings.GetFile(f => f.Name == "Events.json");
                ctx.SeedFromFile(file);
            };

            using (var eventStore = new EventStoreDbContext())
            {
                eventStoreInitializer1.InitializeDatabase(eventStore);

                eventStore.Events.Count().Should().Be(8);
            }
        }
    }
}
