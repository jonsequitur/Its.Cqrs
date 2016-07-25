// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using System.Linq;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [DisableCommandAuthorization]
    [UseInMemoryCommandScheduling]
    [UseInMemoryEventStore]
    public class EventSourcedAggregateTests
    {
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
                new IEvent<Order>[]
                {
                    new Order.CustomerInfoChanged { CustomerName = "joe" },
                    new Order.Cancelled()
                });

            order.CustomerName.Should().Be("joe");
            order.IsCancelled.Should().Be(true);
        }

        [Test]
        public void The_last_event_in_the_sequence_should_win()
        {
            var order = new Order(
                Guid.NewGuid(),
                new[]
                {
                    new Order.CustomerInfoChanged { CustomerName = "bob" },
                    new Order.CustomerInfoChanged { CustomerName = "alice" }
                });

            order.CustomerName.Should().Be("alice");
        }

        [Test]
        public void When_source_event_ids_do_not_match_then_using_them_to_source_an_object_throws()
        {
            Action ctorCall = () =>
            {
                new Order(
                    Guid.NewGuid(),
                    new[]
                    {
                        new Order.CustomerInfoChanged { AggregateId = Guid.NewGuid(), CustomerName = "joe" },
                        new Order.CustomerInfoChanged { AggregateId = Guid.NewGuid(), CustomerName = "hilda" }
                    });
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
                    new[]
                    {
                        new Order.CustomerInfoChanged { SequenceNumber = 1, CustomerName = "joe" },
                        new Order.CustomerInfoChanged { SequenceNumber = 1, CustomerName = "joe" }
                    });
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
                    new IEvent<Order>[]
                    {
                        new Order.CustomerInfoChanged { SequenceNumber = 1, CustomerName = "joe" },
                        new Order.CustomerInfoChanged { SequenceNumber = 2, CustomerName = "joe" },
                        new Order.Cancelled { SequenceNumber = 1, Reason = "just 'cause..." }
                    }
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
               new []  { new Order.Fulfilled() });

            order.IsCancelled.Should().Be(false);

            order.Invoking(o => o.Apply(new Cancel()))
                 .ShouldThrow<CommandValidationException>();

            order.IsCancelled.Should().Be(false);
        }

        [Test]
        public void EventSourcedAggregate_cannot_be_rehydrated_from_an_empty_event_sequence()
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
                                  new[]
                                  {
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 1, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 4, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 8, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 103, ProductName = "foo", Price = 1 }
                                  });

            order.Items.Single().Quantity.Should().Be(4);
        }

        [Test]
        public void When_there_are_gaps_in_the_event_sequence_then_new_events_have_the_correct_sequence_numbers_prior_to_save()
        {
            // arrange
            var id = Guid.NewGuid();
            var order = new Order(id,
                                  new IEvent<Order>[]
                                  {
                                      new Order.Created { AggregateId = id, SequenceNumber = 1, CustomerId = Any.Guid() },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 4, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 8, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 103, ProductName = "foo", Price = 1 }
                                  });

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
                                  new IEvent<Order>[]
                                  {
                                      new Order.Created { AggregateId = id, SequenceNumber = 1, CustomerId = Any.Guid() },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 4, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 8, ProductName = "foo", Price = 1 },
                                      new Order.ItemAdded { AggregateId = id, SequenceNumber = 103, ProductName = "foo", Price = 1 }
                                  });

            // act
            order.Apply(new Cancel());
            order.ConfirmSave();

            // assert
            order.EventHistory.Last().Should().BeOfType<Order.Cancelled>();
            order.EventHistory.Last().SequenceNumber.Should().Be(104);
        }

        [Test]
        public void Id_cannot_be_empty_guid_when_using_event_history_constructor()
        {
            Action create = () => new Order(Guid.Empty, new[] { new Order.CustomerInfoChanged() });

            create.Invoking(c => c())
                  .ShouldThrow<ArgumentException>()
                  .And
                  .Message.Should().Contain("id cannot be Guid.Empty");
        }

        [Test]
        public void When_null_guid_is_passed_to_constructor_without_event_history_then_a_guid_is_chosen()
        {
            var order =  new Order((Guid?)null);

            order.Id.Should().NotBeEmpty();
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

        [Category("Performance")]
        [Test]
        public void When_calling_ctor_of_an_aggregate_with_a_large_number_of_source_events_in_non_incrementing_order_then_the_operation_completes_quickly()
        {
            var count = 100000;
            var largeListOfEvents = Enumerable.Range(1, count)
                                              .Select(i => new PerfTestAggregate.SimpleEvent
                                              {
                                                  SequenceNumber = i
                                              })
                                              .ToList();

            Shuffle(largeListOfEvents, new Random(42));

            var sw = Stopwatch.StartNew();
            var t = new PerfTestAggregate(Guid.NewGuid(), largeListOfEvents);
            sw.Stop();

            Console.WriteLine("Elapsed: {0}ms", sw.ElapsedMilliseconds);
            t.Version.Should().Be(count);
            t.NumberOfUpdatesExecuted.Should().Be(count);
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        }

        private static void Shuffle<T>(IList<T> list, Random randomNumberGenerator)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                var k = randomNumberGenerator.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public class PerfTestAggregate : EventSourcedAggregate<PerfTestAggregate>
        {
            public PerfTestAggregate(Guid id, IEnumerable<IEvent> eventHistory)
                : base(id, eventHistory)
            {
            }

            public class SimpleEvent : Event<PerfTestAggregate>
            {
                public override void Update(PerfTestAggregate order)
                {
                    order.NumberOfUpdatesExecuted++;
                }
            }

            public long NumberOfUpdatesExecuted { get; set; }
        }
    }
}