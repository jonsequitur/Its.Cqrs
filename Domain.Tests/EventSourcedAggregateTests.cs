using System;
using System.Linq;
using System.Reactive.Disposables;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using Microsoft.Its.Domain.Testing;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventSourcedAggregateTests
    {
        private CompositeDisposable disposables;

        [SetUp]
        public void SetUp()
        {
            // disable authorization
            Command<Order>.AuthorizeDefault = (o, c) => true;
            Command<CustomerAccount>.AuthorizeDefault = (o, c) => true;

            disposables = new CompositeDisposable();

            var configurationContext = ConfigurationContext.Establish(
                new Configuration().UseInMemoryEventStore()
                                   .IgnoreScheduledCommands());
            disposables.Add(configurationContext);
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public void When_created_using_new_it_has_a_unique_id_immediately()
        {
            var order = new Order();

            order.Id.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void Properties_can_be_rehydrated_from_an_event_sequence()
        {
            var order = new Order(
                Guid.NewGuid(),
                new Order.CustomerInfoChanged { CustomerName = "joe" },
                new Order.Cancelled());

            order.CustomerName.Should().Be("joe");
            order.IsCancelled.Should().Be(true);
        }

        [Test]
        public void The_last_event_in_the_sequence_should_win()
        {
            var order = new Order(
                Guid.NewGuid(),
                new Order.CustomerInfoChanged { CustomerName = "bob" },
                new Order.CustomerInfoChanged { CustomerName = "alice" });

            order.CustomerName.Should().Be("alice");
        }

        [Test]
        public void When_source_event_ids_do_not_match_then_using_them_to_source_an_object_throws()
        {
            Action ctorCall = () =>
            {
                new Order(
                    Guid.NewGuid(),
                    new Order.CustomerInfoChanged { AggregateId = Guid.NewGuid(), CustomerName = "joe" },
                    new Order.CustomerInfoChanged { AggregateId = Guid.NewGuid(), CustomerName = "hilda" }
                    );
            };

            ctorCall.Invoking(c => c())
                    .ShouldThrow<ArgumentException>()
                    .And
                    .Message.Should().Contain("Inconsistent aggregate ids");
        }

        [Test]
        public void When_a_command_is_applied_its_updates_are_applied_to_the_state_of_the_aggregate()
        {
            var order = new Order();

            order.IsCancelled.Should().Be(false);

            order.Apply(new Cancel());

            order.IsCancelled.Should().Be(true);
        }

        [Test]
        public void When_a_command_fails_then_its_updates_are_not_applied_to_the_aggregate()
        {
            var order = new Order(
                Guid.NewGuid(),
                new Order.Fulfilled());

            order.IsCancelled.Should().Be(false);

            order.Invoking(o => o.Apply(new Cancel()))
                 .ShouldThrow<CommandValidationException>();

            order.IsCancelled.Should().Be(false);
        }

        [Test]
        public void EventSourcedBase_cannot_be_rehydrated_from_an_empty_event_sequence()
        {
            Action ctorCall = () => { new Order(Guid.NewGuid(), new IEvent[0]); };

            ctorCall.Invoking(c => c())
                    .ShouldThrow<ArgumentException>()
                    .And
                    .Message.Should().Contain("Event history is empty");
        }

        [Test]
        public void Gaps_in_the_event_sequence_do_not_cause_incorrect_sourcing()
        {
            var id = Guid.NewGuid();
            var order = new Order(id,
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 1, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 4, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 8, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 103, ProductName = "foo", Price = 1 }
                );

            order.Items.Single().Quantity.Should().Be(4);
        }

        [Test]
        public void When_there_are_gaps_in_the_event_sequence_then_new_events_have_the_correct_sequence_numbers_prior_to_save()
        {
            // arrange
            var id = Guid.NewGuid();
            var order = new Order(id,
                                  new Order.Created { AggregateId = id, SequenceNumber = 1, CustomerId = Any.Guid() },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 4, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 8, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 103, ProductName = "foo", Price = 1 }
                );

            // act
            order.Apply(new Cancel());

            // assert
            order.Version.Should().Be(104);
            order.PendingEvents.Last().SequenceNumber.Should().Be(104);
        }
        
        [Test]
        public void When_there_are_gaps_in_the_event_sequence_then_new_events_have_the_correct_sequence_numbers_after_save()
        {
            // arrange
            var id = Guid.NewGuid();
            var order = new Order(id,
                                  new Order.Created { AggregateId = id, SequenceNumber = 1, CustomerId = Any.Guid() },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 4, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 8, ProductName = "foo", Price = 1 },
                                  new Order.ItemAdded { AggregateId = id, SequenceNumber = 103, ProductName = "foo", Price = 1 }
                );

            // act
            order.Apply(new Cancel());
            order.ConfirmSave();

            // assert
            order.EventHistory.Last().Should().BeOfType<Order.Cancelled>();
            order.EventHistory.Last().SequenceNumber.Should().Be(104);
        }

        [Test]
        public void Id_cannot_be_empty_guid()
        {
            Action create = () => new Order(Guid.Empty);

            create.Invoking(c => c())
                  .ShouldThrow<ArgumentException>()
                  .And
                  .Message.Should().Contain("id cannot be Guid.Empty");
        }

        [Test]
        public void Version_is_initially_0()
        {
            new CustomerAccount().Version.Should().Be(0);
        }

        [Test]
        public void Aggregates_can_be_re_sourced_in_memory_to_older_versions()
        {
            var originalName = Any.FullName();
            var order = new Order(new CreateOrder(originalName));
            order.Apply(new ChangeCustomerInfo
            {
                CustomerName = Any.FullName()
            });

            var orderAtOlderVersion = order.AsOfVersion(1);

            orderAtOlderVersion.CustomerName.Should().Be(originalName);
        }
    }
}