// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    public abstract class EventMigrationTests
    {
        private IEventSourcedRepository<Order> repository;
        private EventMigrator<Order> migrator;
        private Guid aggregateId;

        protected abstract IEventSourcedRepository<Order> CreateRepository();

        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = delegate { return true; };
            repository = CreateRepository();
            migrator = new EventMigrator<Order>(repository);

            var order = new Order().Apply(new AddItem
            {
                ProductName = "Widget",
                Price = 10m,
                Quantity = 2
            });
            repository.Save(order).Wait();
            aggregateId = order.Id;

            repository.GetLatest(aggregateId).Result.EventHistory.Last().Should().BeOfType<Order.ItemAdded>();
        }

        [Test]
        public async Task If_renamed_and_saved_When_the_aggregate_is_sourced_then_the_event_is_the_new_name()
        {
            var order = await repository.GetLatest(aggregateId);
            migrator.PendingRenames.Add(order, order.EventHistory.Last().SequenceNumber, "ItemAdded2");

            await migrator.Save(order);
            (await repository.GetLatest(aggregateId)).EventHistory.Last().Should().BeOfType<Order.ItemAdded2>();
        }

        [Test]
        public async Task If_renamed_but_not_saved_When_the_aggregate_is_sourced_then_the_event_is_the_old_name()
        {
            var order = await repository.GetLatest(aggregateId);
            migrator.PendingRenames.Add(order, order.EventHistory.Last().SequenceNumber, "ItemAdded2");

            (await repository.GetLatest(aggregateId)).EventHistory.Last().Should().BeOfType<Order.ItemAdded>();
        }

        [Test]
        public async Task If_renamed_to_an_unknown_name_When_the_aggregate_is_sourced_then_the_event_is_anonymous()
        {
            var order = await repository.GetLatest(aggregateId);
            migrator.PendingRenames.Add(order, order.EventHistory.Last().SequenceNumber, "ItemAdded (ignored)");

            await migrator.Save(order);
            (await repository.GetLatest(aggregateId)).EventHistory.Last().GetType().Name.Should().Be("AnonymousEvent`1");
        }

        [Test]
        public async Task If_an_unrecognized_event_is_renamed_then_a_useful_exception_is_thrown()
        {
            var order = await repository.GetLatest(aggregateId);
            migrator.PendingRenames.Add(order, 99999, "ItemAdded (ignored)");
            migrator.Invoking(_ => _.Save(order).Wait())
                    .ShouldThrow<EventMigrator.SequenceNumberNotFoundException>()
                    .And.Message.Should().StartWith("Migration failed, because no event with sequence number 99999 on aggregate ");
        }

        [TestFixture]
        public class Given_an_EventSourcedRepository_that_does_not_support_migration
        {
            private class EventSourceRepositoryWithoutMigrationSupport : IEventSourcedRepository<Order> //, IMigratableEventSourcedRepository<Order> 
            {
                Task<Order> IEventSourcedRepository<Order>.GetLatest(Guid aggregateId)
                {
                    throw new NotImplementedException();
                }

                Task<Order> IEventSourcedRepository<Order>.GetVersion(Guid aggregateId, long version)
                {
                    throw new NotImplementedException();
                }

                Task<Order> IEventSourcedRepository<Order>.GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
                {
                    throw new NotImplementedException();
                }

                Task IEventSourcedRepository<Order>.Save(Order aggregate)
                {
                    throw new NotImplementedException();
                }

                Task IEventSourcedRepository<Order>.Refresh(Order aggregate)
                {
                    throw new NotImplementedException();
                }
            }

            [Test]
            public void EventMigrator_refuses_to_accept_it()
            {
                Action action = delegate { new EventMigrator<Order>(new EventSourceRepositoryWithoutMigrationSupport()); };
                action.ShouldThrow<EventMigrator.RepositoryMustSupportMigrationsException>()
                      .And.Message.Should()
                      .Be("Repository type 'EventSourceRepositoryWithoutMigrationSupport' cannot be used for migrations because it does not implement 'IMigratableEventSourcedRepository`1'");
            }
        }
    }
}