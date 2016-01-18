// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

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

            var configurationContext = ConfigurationContext
                .Establish(new Configuration()
                               .UseInMemoryEventStore()
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
        public void When_source_events_contain_events_with_the_same_sequence_number_and_the_same_types_then_using_them_to_source_an_object_throws()
        {
            Action ctorCall = () =>
            {
                new Order(
                    Guid.NewGuid(),
                    new Order.CustomerInfoChanged { SequenceNumber = 1, CustomerName = "joe" },
                    new Order.CustomerInfoChanged { SequenceNumber = 1, CustomerName = "joe" }
                    );
            };

            ctorCall.Invoking(c => c())
                    .ShouldThrow<ArgumentException>()
                    .And
                    .Message.Should().Contain("Event with SequenceNumber 1 is already present in the sequence.");
        }

        [Test]
        public void When_source_events_contain_events_with_the_same_sequence_number_but_different_types_then_using_them_to_source_an_object_throws()
        {
            Action ctorCall = () =>
            {
                new Order(
                    Guid.NewGuid(),
                    new Order.CustomerInfoChanged { SequenceNumber = 1, CustomerName = "joe" },
                    new Order.CustomerInfoChanged { SequenceNumber = 2, CustomerName = "joe" },
                    new Order.Cancelled { SequenceNumber = 1, Reason = "just 'cause..."}
                    );
            };

            ctorCall.Invoking(c => c())
                    .ShouldThrow<ArgumentException>()
                    .And
                    .Message.Should().Contain("Event with SequenceNumber 1 is already present in the sequence.");
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
        public void EventSourcedBase_can_be_rehydrated_from_an_empty_event_sequence_when_using_a_snapshot()
        {
            Action ctorCall = () =>
            {
                new CustomerAccount(new CustomerAccountSnapshot
                {
                    AggregateId = Any.Guid(),
                    EmailAddress = Any.Email(),
                    Version = 12
                }, new IEvent[0]);
            };

            ctorCall.Invoking(c => c())
                    .ShouldNotThrow<Exception>();
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
        public async Task When_instantiated_from_a_snapshot_the_aggregate_version_is_correct()
        {
            var customerAccount = new CustomerAccount(new CustomerAccountSnapshot
            {
                AggregateId = Any.Guid(),
                EmailAddress = Any.Email(),
                Version = 12
            });

            customerAccount.Version.Should().Be(12);
        }

        [Test]
        public async Task An_aggregate_instantiated_from_a_snapshot_adds_new_events_at_the_correct_sequence_number()
        {
            var customerAccount = new CustomerAccount(new CustomerAccountSnapshot
            {
                AggregateId = Any.Guid(),
                EmailAddress = Any.Email(),
                Version = 12
            });

            customerAccount.Apply(new RequestNoSpam());

            customerAccount.Version.Should().Be(13);
            customerAccount.PendingEvents.Last().SequenceNumber.Should().Be(13);
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

        [Test]
        public async Task Attempting_to_re_source_an_aggregate_that_was_instantiated_using_a_snapshot_succeeds_if_the_specified_version_is_at_or_after_the_snapshot()
        {
            var originalEmail = Any.Email();
            var customerAccount = new CustomerAccount(new CustomerAccountSnapshot
            {
                AggregateId = Any.Guid(),
                Version = 10,
                EmailAddress = originalEmail
            });

            customerAccount.Apply(new ChangeEmailAddress(Any.Email()));

            var accountAtv10 = customerAccount.AsOfVersion(10);

            accountAtv10.Version.Should().Be(10);
            accountAtv10.EmailAddress.Should().Be(originalEmail);
        }

        [Test]
        public async Task Attempting_to_re_source_an_aggregate_that_was_instantiated_using_a_snapshot_throws_if_the_specified_version_is_prior_to_the_snapshot()
        {
            var originalEmail = Any.Email();
            var customerAccount = new CustomerAccount(new CustomerAccountSnapshot
            {
                AggregateId = Any.Guid(),
                Version = 10,
                EmailAddress = originalEmail
            });

            Action rollBack = () => customerAccount.AsOfVersion(9);

            rollBack.ShouldThrow<InvalidOperationException>()
                    .And
                    .Message
                    .Should()
                    .Contain("Snapshot version is later than specified version.");
        }

        [Test]
        public async Task Accessing_event_history_from_an_aggregate_instantiated_using_a_snapshot_throws()
        {
            var customerAccount = new CustomerAccount(new CustomerAccountSnapshot
            {
                AggregateId = Any.Guid(),
                Version = 10,
                EmailAddress = Any.Email()
            });

            Action getEvents = () => customerAccount.Events();

            getEvents.ShouldThrow<InvalidOperationException>()
                     .And
                     .Message
                     .Should()
                     .Contain("Aggregate was sourced from a snapshot, so event history is unavailable.");
        }

        [Test]
        public async Task Snapshots_include_etags_for_events_that_have_been_persisted_to_the_event_store()
        {
            var etag = Guid.NewGuid().ToString().ToETag();

            var configuration = Configuration.Current;
            var addPlayer = new MarcoPoloPlayerWhoIsNotIt.JoinGame
            {
                IdOfPlayerWhoIsIt = Any.Guid(),
                ETag = etag
            };

            var player = await new MarcoPoloPlayerWhoIsNotIt()
                .ApplyAsync(addPlayer);
            await configuration.Repository<MarcoPoloPlayerWhoIsNotIt>().Save(player);

            await configuration.SnapshotRepository().SaveSnapshot(player);

            var snapshot = await configuration.SnapshotRepository()
                                              .GetSnapshot(player.Id);

            snapshot.ETags
                    .MayContain(etag)
                    .Should()
                    .BeTrue();
        }

        [Test]
        public async Task When_sourced_from_a_snapshot_and_applying_a_command_that_is_already_in_the_bloom_filter_then_a_precondition_check_is_used_to_rule_out_false_positives()
        {
            var verifierWasCalled = false;

            var preconditionVerifier = new TestCommandPreconditionVerifier(() =>
            {
                verifierWasCalled = true;
                return true;
            });

            var configuration = Configuration.Current;

            configuration.UseDependency<ICommandPreconditionVerifier>(_ => preconditionVerifier);

            var etag = Guid.NewGuid().ToString().ToETag();

            var addPlayer = new MarcoPoloPlayerWhoIsNotIt.JoinGame
            {
                IdOfPlayerWhoIsIt = Any.Guid(),
                ETag = etag
            };

            var player = await new MarcoPoloPlayerWhoIsNotIt()
                .ApplyAsync(addPlayer);
            await configuration.Repository<MarcoPoloPlayerWhoIsNotIt>().Save(player);

            // don't call player.ConfirmSave

            await configuration.SnapshotRepository()
                               .SaveSnapshot(player);

            var snapshot = await configuration.SnapshotRepository().GetSnapshot(player.Id);

            player = new MarcoPoloPlayerWhoIsNotIt(snapshot);

            player.HasETag(etag).Should().BeTrue();
            verifierWasCalled.Should().BeTrue();
        }

        private class TestCommandPreconditionVerifier : ICommandPreconditionVerifier
        {
            private readonly Func<bool> hasBeenApplied;

            public TestCommandPreconditionVerifier(Func<bool> hasBeenApplied)
            {
                if (hasBeenApplied == null)
                {
                    throw new ArgumentNullException("hasBeenApplied");
                }
                this.hasBeenApplied = hasBeenApplied;
            }

            public async Task<bool> HasBeenApplied(Guid aggregateId, string etag)
            {
                return hasBeenApplied();
            }
        }
    }
}