// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class InMemoryEventStoreTests
    {
        private CompositeDisposable disposables;

        [SetUp]
        public void SetUp()
        {
            var configuration = new Configuration()
                .UseInMemoryEventStore();

            disposables = new CompositeDisposable
            {
                ConfigurationContext.Establish(configuration),
                configuration
            };
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public async Task Events_can_be_added_to_the_context_and_queried_back()
        {
            var aggregateId = Any.Guid();

            var storableEvent = new Order.Created
            {
                CustomerName = Any.FullName(),
                AggregateId = aggregateId
            }.ToStorableEvent();

            var eventStream = new InMemoryEventStream();

            using (var db = new InMemoryEventStoreDbContext(eventStream))
            {
                db.Events.Add(storableEvent);

                await db.SaveChangesAsync();

                var orderCreated = db.Events.Single(e => e.AggregateId == aggregateId);

                orderCreated.Should().NotBeNull();
            }
        }

        [Test]
        public async Task Events_in_the_currently_configured_event_stream_can_be_queried_from_the_In_memory_context()
        {
            var aggregateId = Any.Guid();

            var storableEvent = new Order.Created
            {
                CustomerName = Any.FullName(),
                AggregateId = aggregateId
            }.ToStorableEvent();

            var eventStream = new InMemoryEventStream();
            await eventStream.Append(new[] { storableEvent.ToStoredEvent() });

            Configuration.Current.UseDependency(_ => eventStream);

            using (var db = new InMemoryEventStoreDbContext())
            {
                var orderCreated = db.Events.Single(e => e.AggregateId == aggregateId);

                orderCreated.Should().NotBeNull();
            }
        }
    }
}