// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class EventSourcedAggregateSnapshottingTests
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
        public void An_aggregate_cannot_be_sourced_from_the_wrong_snapshot_type()
        {
            // arrange
            var order = new Order(new CreateOrder(Any.FullName()));
            order.ConfirmSave();
            var snapshot = order.CreateSnapshot();

            // act
            Action restoreToWrongType = () => new CustomerAccount(snapshot);

            // assert
            restoreToWrongType.ShouldThrow<ArgumentException>()
                              .And
                              .Message
                              .Should()
                              .Be("Snapshotted Order cannot be used to instantiate a CustomerAccount");
        }

        [Test]
        public void Accessing_event_history_from_an_aggregate_instantiated_using_a_snapshot_throws()
        {
            // arrange
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress(Any.Email()));
            account.ConfirmSave();
            var snapshot = account
                .CreateSnapshot();
            snapshot.Version = 10;
            account = new CustomerAccount(snapshot);

            // act
            Action getEvents = () => account.Events();

            // assert
            getEvents.ShouldThrow<InvalidOperationException>()
                     .And
                     .Message
                     .Should()
                     .Contain("Aggregate was sourced from a snapshot, so event history is unavailable.");
        }

        [Test]
        public async Task When_sourced_from_a_snapshot_and_applying_a_command_that_is_already_in_the_bloom_filter_then_a_precondition_check_is_used_to_rule_out_false_positives()
        {
            var verifierWasCalled = false;

            var preconditionVerifier = new TestEventStoreETagChecker(() =>
            {
                verifierWasCalled = true;
                return true;
            });

            var configuration = Configuration.Current;

            configuration.UseDependency<IETagChecker>(_ => preconditionVerifier);

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

        [Test]
        public void Attempting_to_re_source_an_aggregate_that_was_instantiated_using_a_snapshot_throws_if_the_specified_version_is_prior_to_the_snapshot()
        {
            // arrange
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress(Any.Email()));
            account.ConfirmSave();
            var snapshot = account
                .CreateSnapshot();
            snapshot.Version = 10;

            account = new CustomerAccount(snapshot);

            // act
            Action rollBack = () => account.AsOfVersion(9);

            // assert
            rollBack.ShouldThrow<InvalidOperationException>()
                    .And
                    .Message
                    .Should()
                    .Contain("Snapshot version is later than specified version.");
        }

        [Test]
        public void When_instantiated_from_a_snapshot_the_aggregate_version_is_correct()
        {
            // arrange
            var customerAccount = new CustomerAccount()
                .Apply(new ChangeEmailAddress(Any.Email()));
            customerAccount.ConfirmSave();
            var snapshot = customerAccount.CreateSnapshot();
            snapshot.Version = 12;

            // act
            var accountFromSnapshot = new CustomerAccount(snapshot);

            // assert
            accountFromSnapshot.Version.Should().Be(12);
        }

        [Test]
        public void An_aggregate_instantiated_from_a_snapshot_adds_new_events_at_the_correct_sequence_number()
        {
            // arrange
            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress(Any.Email()));
            account.ConfirmSave();
            var snapshot = account.CreateSnapshot();

            snapshot.Version = 12;

            // act
            var accountFromSnapshot = new CustomerAccount(snapshot);
            accountFromSnapshot.Apply(new RequestNoSpam());

            // assert
            accountFromSnapshot.Version.Should().Be(13);
            accountFromSnapshot.PendingEvents.Last().SequenceNumber.Should().Be(13);
        }

        [Test]
        public void EventSourcedAggregate_can_be_rehydrated_from_an_empty_event_sequence_when_using_a_snapshot()
        {
            // arrange
            var emailAddress = Any.Email();

            var customerAccount = new CustomerAccount()
                .Apply(new ChangeEmailAddress(emailAddress));
            customerAccount.ConfirmSave();
            var snapshot = customerAccount.CreateSnapshot();

            //act
            var fromSnapshot = new CustomerAccount(snapshot);

            // assert
            fromSnapshot.EmailAddress
                        .Should()
                        .Be(emailAddress);
        }

        [Test]
        public void Attempting_to_re_source_an_aggregate_that_was_instantiated_using_a_snapshot_succeeds_if_the_specified_version_is_at_or_after_the_snapshot()
        {
            // arrange
            var originalEmail = Any.Email();

            var account = new CustomerAccount()
                .Apply(new ChangeEmailAddress(originalEmail));

            account.ConfirmSave();

            var snapshot = account.CreateSnapshot();
            snapshot.Version = 10;
            account = new CustomerAccount(snapshot);

            account.Apply(new ChangeEmailAddress(Any.Email()));

            // act
            var accountAtVersion10 = account.AsOfVersion(10);

            // arrange
            accountAtVersion10.Version.Should().Be(10);
            accountAtVersion10.EmailAddress.Should().Be(originalEmail);
        }

        [Test]
        public void Aggregates_with_reference_cycles_can_be_snapshotted()
        {
            // arrange
            var node1 = new Node();
            var node2 = new Node();
            node1.Next = node2;
            node2.Next = node1;
            var aggregate = new ReferenceCycleTestAggregate
            {
                Node = node1
            };

            // act
            var snapshot = aggregate.CreateSnapshot();
            var fromSnapshot = new ReferenceCycleTestAggregate(snapshot);

            // assert
            var firstNode = fromSnapshot.Node;
            firstNode.Next.Next.Should().BeSameAs(firstNode);
        }

        public class ReferenceCycleTestAggregate : EventSourcedAggregate<ReferenceCycleTestAggregate>
        {
            public ReferenceCycleTestAggregate(Guid? id = null) : base(id)
            {
            }

            public ReferenceCycleTestAggregate(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
            {
            }

            public ReferenceCycleTestAggregate(ISnapshot snapshot, IEnumerable<IEvent> eventHistory = null) : base(snapshot, eventHistory)
            {
            }

            public Node Node { get; set; }
        }

        public class Node
        {
            public Node Next { get; set; }
        }

        private class TestEventStoreETagChecker : IETagChecker
        {
            private readonly Func<bool> hasBeenApplied;

            public TestEventStoreETagChecker(Func<bool> hasBeenApplied)
            {
                if (hasBeenApplied == null)
                {
                    throw new ArgumentNullException(nameof(hasBeenApplied));
                }
                this.hasBeenApplied = hasBeenApplied;
            }

            public async Task<bool> HasBeenRecorded(string scope, string etag) =>
                hasBeenApplied();
        }
    }
}