// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;
using Sample.Domain.Projections;
using Assert = NUnit.Framework.Assert;
using Its.Log.Instrumentation;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public  class ReadModelCatchupTests : ReadModelCatchupTest
    {
    }

    [Category("Catchups")]
    [TestFixture]
    public abstract class ReadModelCatchupTest : EventStoreDbTest
    {
        private readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(5);

        [Test]
        public void ReadModelCatchup_only_queries_events_since_the_last_consumed_event_id()
        {
            var bus = new FakeEventBus();
            var repository = new SqlEventSourcedRepository<Order>(bus);

            // save the order with no projectors running
            var order = new Order();
            order.Apply(new AddItem
            {
                Price = 1m,
                ProductName = "Widget"
            });
            repository.Save(order);

            // subscribe one projector for catchup
            var projector1 = new Projector1();
            using (var catchup = CreateReadModelCatchup(projector1))
            {
                catchup.Progress.ForEachAsync(s => Console.WriteLine(s));
                catchup.Run();
            }

            order.Apply(new AddItem
            {
                Price = 1m,
                ProductName = "Widget"
            });
            repository.Save(order);

            // subscribe both projectors
            var projector2 = new Projector2();
            using (var catchup = CreateReadModelCatchup(projector1, projector2))
            {
                catchup.Progress.ForEachAsync(s => Console.WriteLine(s));
                catchup.Run();
            }

            projector1.CallCount.Should().Be(2, "A given event should only be passed to a given projector once");
            projector2.CallCount.Should().Be(2, "A projector should be passed events it has not previously seen.");
        }

        [Test]
        public void ReadModelCatchup_does_not_query_events_that_no_subscribed_projector_is_interested_in()
        {
            Events.Write(100, _ => Events.Any());

            var projector1 = new Projector1();
            var projector2 = Projector.Create<CustomerAccount.EmailAddressChanged>(e => { });

            StorableEvent extraneousEvent = null;

            using (var catchup = CreateReadModelCatchup(projector1, projector2))
            using (var eventStore = new EventStoreDbContext())
            {
                var eventsQueried = 0;
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s =>
                       {
                           var eventId = s.CurrentEventId;
                           var @event = eventStore.Events.Single(e => e.Id == eventId);
                           if (@event.Type != "EmailAddressChanged" && @event.Type != "ItemAdded")
                           {
                               extraneousEvent = @event;
                               catchup.Dispose();
                           }
                           eventsQueried++;
                       });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            if (extraneousEvent != null)
            {
                Assert.Fail(string.Format("Found an event that should not have been queried from the event store: {0}:{1} (#{2})", extraneousEvent.StreamName, extraneousEvent.Type,
                                          extraneousEvent.Id));
            }
        }

        [Test]
        public void ReadModelCatchup_queries_all_events_if_IEvent_is_subscribed()
        {
            Events.Write(100, _ => Events.Any());

            var projector = Projector.Create<IEvent>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s => { eventsQueried++; });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            eventsQueried.Should().Be(100);
        }

        [Test]
        public void ReadModelCatchup_queries_all_events_if_Event_is_subscribed()
        {
            Events.Write(100, _ => Events.Any());

            var projector = Projector.Create<Event>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s => { eventsQueried++; });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            eventsQueried.Should().Be(100);
        }

        [Test]
        public void ReadModelCatchup_queries_all_events_for_the_aggregate_if_IEvent_T_is_subscribed()
        {
            var orderEvents = 0;
            Events.Write(100, _ =>
            {
                var @event = Events.Any();
                if (@event.AggregateType() == typeof (Order))
                {
                    orderEvents++;
                }
                return @event;
            });

            var projector = Projector.Create<IEvent<Order>>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s => { eventsQueried++; });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            eventsQueried.Should().Be(orderEvents);
        }

        [Test]
        public void ReadModelCatchup_queries_all_events_for_the_aggregate_if_Event_T_is_subscribed()
        {
            var orderEvents = 0;
            Events.Write(100, _ =>
            {
                var @event = Events.Any();
                if (@event.AggregateType() == typeof (Order))
                {
                    orderEvents++;
                }
                return @event;
            });

            var projector = Projector.Create<IEvent<Order>>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s => { eventsQueried++; });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            eventsQueried.Should().Be(orderEvents);
        }

        [Test]
        public void ReadModelCatchup_queries_event_subtypes_correctly()
        {
            var subclassedEventsWritten = 0;
            Events.Write(100, _ =>
            {
                var @event = Any.Bool()
                                 ? Events.Any()
                                 : new CustomerAccount.OrderShipConfirmationEmailSent();

                if (@event is EmailSent)
                {
                    subclassedEventsWritten++;
                }

                return @event;
            });

            var projector = Projector.Create<EmailSent>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s => { eventsQueried++; });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            eventsQueried.Should().Be(subclassedEventsWritten);
        }

        [Test]
        public void ReadModelCatchup_queries_scheduled_commands_if_IScheduledCommand_is_subscribed()
        {
            var scheduledCommandsWritten = 0;
            var scheduledCommandsQueried = 0;
            Events.Write(50, _ =>
            {
                if (Any.Bool())
                {
                    return Events.Any();
                }

                scheduledCommandsWritten++;
                return new CommandScheduled<Order>
                {
                    Command = new Ship(),
                    DueTime = DateTimeOffset.UtcNow
                };
            });

            var projector = Projector.Create<IScheduledCommand<Order>>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            using (var eventStore = new EventStoreDbContext())
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s =>
                       {
                           var eventId = s.CurrentEventId;
                           var @event = eventStore.Events.Single(e => e.Id == eventId);
                           if (@event.Type.StartsWith("Scheduled:"))
                           {
                               scheduledCommandsQueried++;
                           }
                           eventsQueried++;
                       });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            scheduledCommandsQueried.Should().Be(scheduledCommandsWritten);
        }

        [Test]
        public void ReadModelCatchup_queries_scheduled_commands_if_CommandScheduled_is_subscribed()
        {
            var scheduledCommandsWritten = 0;
            var scheduledCommandsQueried = 0;
            Events.Write(50, _ =>
            {
                if (Any.Bool())
                {
                    return Events.Any();
                }

                scheduledCommandsWritten++;
                return new CommandScheduled<Order>
                {
                    Command = new Ship(),
                    DueTime = DateTimeOffset.UtcNow
                };
            });

            var projector = Projector.Create<CommandScheduled<Order>>(e => { }).Named(MethodBase.GetCurrentMethod().Name);
            var eventsQueried = 0;

            using (var catchup = CreateReadModelCatchup(projector))
            using (var eventStore = new EventStoreDbContext())
            {
                catchup.Progress
                       .Where(s => !s.IsStartOfBatch)
                       .ForEachAsync(s =>
                       {
                           var eventId = s.CurrentEventId;
                           var @event = eventStore.Events.Single(e => e.Id == eventId);
                           if (@event.Type.StartsWith("Scheduled:"))
                           {
                               scheduledCommandsQueried++;
                           }
                           eventsQueried++;
                       });
                catchup.DisposeAfter(r => r.Run());
                Console.WriteLine(new { eventsQueried });
            }

            scheduledCommandsQueried.Should().Be(scheduledCommandsWritten);
        }

        [Test]
        public void ReadModelCatchup_StartAtEventId_can_be_used_to_avoid_requery_of_previous_events()
        {
            var lastEventId = Events.Write(50, _ => Events.Any());

            var eventsProjected = 0;

            var projector = Projector.Create<Event>(e => { eventsProjected++; }).Named(MethodBase.GetCurrentMethod().Name);

            using (var catchup = new ReadModelCatchup(projector)
            {
                StartAtEventId = lastEventId - 20
            })
            {
                catchup.Run();
            }

            eventsProjected.Should().Be(21);
        }

        [Test]
        public void When_Run_is_called_while_already_running_then_it_skips_the_run()
        {
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            new Order().Apply(new AddItem
            {
                Price = 1m,
                ProductName = MethodBase.GetCurrentMethod().Name
            });
            repository.Save(new Order());
            var mre = new ManualResetEventSlim();
            var progress = new List<ReadModelCatchupStatus>();

            var signal = new ReplaySubject<Unit>();
            var projector = new Projector<Order.ItemAdded>(() => new ReadModelDbContext())
            {
                OnUpdate = (work, e) =>
                {
                    signal.OnNext(Unit.Default);
                    mre.Wait(20000);
                }
            };

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Progress.ForEachAsync(s =>
                {
                    progress.Add(s);
                    Console.WriteLine(s);
                });

                Task.Run(() => { catchup.Run(); });

                mre.Set();
                catchup.Run();
            }

            progress.Should().ContainSingle(s => s.IsStartOfBatch);
        }

        [Test]
        public void When_a_projector_update_fails_then_an_entry_is_added_to_EventHandlingErrors()
        {
            // arrange
            var errorMessage = Any.Paragraph(10);
            var productName = Any.Paragraph();
            var projector = new Projector<Order.ItemAdded>(() => new ReadModelDbContext())
            {
                OnUpdate = (work, e) => { throw new Exception(errorMessage); }
            };
            var order = new Order();
            var repository = new SqlEventSourcedRepository<Order>();
            order.Apply(new AddItem
            {
                Price = 1m,
                ProductName = productName
            });
            repository.Save(order);

            // act
            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            // assert
            using (var db = new ReadModelDbContext())
            {
                var error = db.Set<EventHandlingError>().Single(e => e.AggregateId == order.Id);
                error.StreamName.Should().Be("Order");
                error.EventTypeName.Should().Be("ItemAdded");
                error.SerializedEvent.Should().Contain(productName);
                error.Error.Should().Contain(errorMessage);
            }
        }

        [Test]
        public void Events_that_cannot_be_deserialized_to_the_expected_type_are_logged_as_EventHandlingErrors()
        {
            var badEvent = new StorableEvent
            {
                Actor = Any.Email(),
                StreamName = typeof (Order).Name,
                Type = typeof (Order.ItemAdded).Name,
                Body = new { Price = "oops this is not a number" }.ToJson(),
                SequenceNumber = Any.PositiveInt(),
                AggregateId = Any.Guid(),
                UtcTime = DateTime.UtcNow
            };

            using (var eventStore = new EventStoreDbContext())
            {
                eventStore.Events.Add(badEvent);
                eventStore.SaveChanges();
            }

            using (var catchup = CreateReadModelCatchup(new Projector1()))
            {
                catchup.Run();
            }

            using (var readModels = new ReadModelDbContext())
            {
                var failure = readModels.Set<EventHandlingError>()
                                        .OrderByDescending(e => e.Id)
                                        .First(e => e.AggregateId == badEvent.AggregateId);
                failure.Error.Should().Contain("JsonReaderException");
                failure.SerializedEvent.Should().Contain(badEvent.Body);
                failure.Actor.Should().Be(badEvent.Actor);
                failure.OriginalId.Should().Be(badEvent.Id);
                failure.AggregateId.Should().Be(badEvent.AggregateId);
                failure.SequenceNumber.Should().Be(badEvent.SequenceNumber);
                failure.StreamName.Should().Be(badEvent.StreamName);
                failure.EventTypeName.Should().Be(badEvent.Type);
            }
        }

        [Test]
        public void When_an_exception_is_thrown_during_a_read_model_update_then_it_is_logged_on_its_bus()
        {
            var projector = new Projector<Order.ItemAdded>(() => new ReadModelDbContext())
            {
                OnUpdate = (work, e) => { throw new Exception("oops!"); }
            };

            var itemAdded = new Order.ItemAdded
            {
                AggregateId = Any.Guid(),
                SequenceNumber = 1,
                ProductName = Any.AlphanumericString(10, 20),
                Price = Any.Decimal(0.01m),
                Quantity = 100
            };

            var errors = new List<Domain.EventHandlingError>();
            using (var readModelCatchup = CreateReadModelCatchup(projector))
            using (var db = new EventStoreDbContext())
            {
                db.Events.Add(itemAdded.ToStorableEvent());
                db.SaveChanges();
                readModelCatchup.EventBus.Errors.Subscribe(errors.Add);
                readModelCatchup.Run();
            }

            var error = errors.Single(e => e.AggregateId == itemAdded.AggregateId);
            error.SequenceNumber.Should().Be(itemAdded.SequenceNumber);
            error.Event
                 .ShouldHave()
                 .Properties(p => p.SequenceNumber)
                 .EqualTo(itemAdded);
        }

        [Ignore("Test needs rebuilding")]
        [Test]
        public void Database_command_timeouts_during_catchup_do_not_interrupt_catchup()
        {
            // reset read model tracking to 0
            new ReadModelDbContext().DisposeAfter(c =>
            {
                var projectorName = ReadModelInfo.NameForProjector(new Projector<Order.CustomerInfoChanged>(() => new ReadModelDbContext()));
                c.Set<ReadModelInfo>()
                 .SingleOrDefault(i => i.Name == projectorName)
                 .IfNotNull()
                 .ThenDo(i => { i.CurrentAsOfEventId = 0; });
                c.SaveChanges();
            });

            var exceptions = new Stack<Exception>(Enumerable.Range(1, 2)
                                                            .Select(_ => new InvalidOperationException("Invalid attempt to call IsDBNull when reader is closed.")));

            int count = 0;
            var flakyEvents = new FlakyEventStream(
                Enumerable.Range(1, 1000)
                          .Select(i => new StorableEvent
                          {
                              AggregateId = Any.Guid(),
                              Body = new Order.CustomerInfoChanged { CustomerName = i.ToString() }.ToJson(),
                              SequenceNumber = i,
                              StreamName = typeof (Order).Name,
                              Timestamp = DateTimeOffset.Now,
                              Type = typeof (Order.CustomerInfoChanged).Name,
                              Id = i
                          }).ToArray(),
                startFlakingOnEnumeratorNumber: 2,
                doSomethingFlaky: i =>
                {
                    if (count++ > 50)
                    {
                        count = 0;
                        if (exceptions.Any())
                        {
                            throw exceptions.Pop();
                        }
                    }
                });

            var names = new HashSet<string>();

            var projector = new Projector<Order.CustomerInfoChanged>(() => new ReadModelDbContext())
            {
                OnUpdate = (work, e) => names.Add(e.CustomerName)
            };

            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            projector.CallCount.Should().Be(1000);
            names.Count.Should().Be(1000);
        }

        [Test]
        public void Insertion_of_new_events_during_catchup_does_not_interrupt_catchup()
        {
            var barrier = new Barrier(2);

            // preload some events for the catchup. replay will hit the barrier on the last one.
            var order = new Order();
            Action addEvent = () => order.Apply(new AddItem
            {
                Quantity = 1,
                ProductName = "Penny candy",
                Price = .01m
            });
            Enumerable.Range(1, 100).ForEach(_ => addEvent());
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            repository.Save(order);

            // queue the catchup on a background task
            Task.Run(() =>
            {
                var projector = new Projector1
                {
                    OnUpdate = (work, e) =>
                    {
                        if (e.SequenceNumber == 10)
                        {
                            Console.WriteLine("pausing read model catchup");
                            barrier.SignalAndWait(MaxWaitTime); //1
                            barrier.SignalAndWait(MaxWaitTime); //2
                            Console.WriteLine("resuming read model catchup");
                        }
                    }
                };

                using (var db = new EventStoreDbContext())
                using (var catchup = CreateReadModelCatchup(projector))
                {
                    var events = db.Events.Where(e => e.Id > HighestEventId);
                    Console.WriteLine(string.Format("starting read model catchup for {0} events", events.Count()));
                    catchup.Run();
                    Console.WriteLine("done with read model catchup");
                    barrier.SignalAndWait(MaxWaitTime); //3
                }
            });

            Console.WriteLine("queued read model catchup task");
            barrier.SignalAndWait(MaxWaitTime); //1

            new EventStoreDbContext().DisposeAfter(c =>
            {
                Console.WriteLine("adding one more event, bypassing read model tracking");
                c.Events.Add(new Order.ItemAdded
                {
                    AggregateId = Guid.NewGuid(),
                    SequenceNumber = 1
                }.ToStorableEvent());
                c.SaveChanges();
                Console.WriteLine("done adding one more event");
            });

            barrier.SignalAndWait(MaxWaitTime); //2
            barrier.SignalAndWait(MaxWaitTime); //3

            // check that everything worked:
            var projector2 = new Projector1();
            var projectorName = ReadModelInfo.NameForProjector(projector2);
            using (var readModels = new ReadModelDbContext())
            {
                var readModelInfo = readModels.Set<ReadModelInfo>().Single(i => i.Name == projectorName);

                readModelInfo.CurrentAsOfEventId.Should().Be(HighestEventId + 101);

                using (var catchup = CreateReadModelCatchup(projector2))
                {
                    catchup.Run();
                }

                readModels.Entry(readModelInfo).Reload();
                readModelInfo.CurrentAsOfEventId.Should().Be(HighestEventId + 102);
            }
        }

        [Test]
        public void When_not_using_Update_then_failed_writes_do_not_interrupt_catchup()
        {
            // arrange
            // preload some events for the catchup. replay will hit the barrier on the last one.
            var order = new Order();
            var productName = Any.Paragraph(3);
            Action addEvent = () => order.Apply(new AddItem
            {
                Quantity = 1,
                ProductName = productName,
                Price = .01m
            });
            Enumerable.Range(1, 30).ForEach(_ => addEvent());
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            repository.Save(order);
            int count = 0;
            var projector = new Projector1
            {
                OnUpdate = (work, e) =>
                {
                    using (var db = new ReadModelDbContext())
                    {
                        // throw one exception in the middle
                        if (count++ == 15)
                        {
                            throw new Exception("drat!");
                        }
                        db.SaveChanges();
                    }
                }
            };

            // act
            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            // assert
            count.Should().Be(30);
        }

        [Test]
        public void When_using_Update_then_failed_writes_do_not_interrupt_catchup()
        {
            // preload some events for the catchup. replay will hit the barrier on the last one.
            var order = new Order();
            var productName = Any.Paragraph(4);
            Action addEvent = () => order.Apply(new AddItem
            {
                Quantity = 1,
                ProductName = productName,
                Price = .01m
            });
            Enumerable.Range(1, 30).ForEach(_ => addEvent());
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            repository.Save(order);
            int count = 0;
            Projector1 projector = null;
            projector = new Projector1
            {
                OnUpdate = (_, e) =>
                {
                    using (var work = projector.Update())
                    {
                        var db = work.Resource<ReadModelDbContext>();
                        if (count++ == 15)
                        {
                            // do something that will trigger a db exception when the UnitOfWork is committed
                            var inventory = db.Set<ProductInventory>();
                            inventory.Add(new ProductInventory
                            {
                                ProductName = e.ProductName,
                                QuantityReserved = e.Quantity
                            });
                            inventory.Add(new ProductInventory
                            {
                                ProductName = e.ProductName,
                                QuantityReserved = e.Quantity
                            });
                        }
                        work.VoteCommit();
                    }
                }
            };

            // act
            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            // assert
            count.Should().Be(30);
        }

        [Test]
        public void When_using_Update_then_failed_writes_are_logged_to_EventHandlingErrors()
        {
            // preload some events for the catchup. replay will hit the barrier on the last one.
            var order = new Order();
            var productName = Any.Paragraph(4);
            order.Apply(new AddItem
            {
                Quantity = 1,
                ProductName = productName,
                Price = .01m
            });
            var repository = new SqlEventSourcedRepository<Order>(new FakeEventBus());
            repository.Save(order);
            Projector1 projector = null;
            projector = new Projector1
            {
                OnUpdate = (_, e) =>
                {
                    using (var work = projector.Update())
                    {
                        var db = work.Resource<ReadModelDbContext>();
                        // do something that will trigger a db exception when the UnitOfWork is committed
                        var inventory = db.Set<ProductInventory>();
                        inventory.Add(new ProductInventory
                        {
                            ProductName = e.ProductName,
                            QuantityReserved = e.Quantity
                        });
                        inventory.Add(new ProductInventory
                        {
                            ProductName = e.ProductName,
                            QuantityReserved = e.Quantity
                        });
                        work.VoteCommit();
                    }
                }
            };

            // act
            using (var catchup = CreateReadModelCatchup(projector))
            {
                catchup.Run();
            }

            // assert
            using (var db = new ReadModelDbContext())
            {
                var error = db.Set<EventHandlingError>().Single(e => e.AggregateId == order.Id);
                error.Error.Should()
                     .Contain(
                         string.Format(
                             "Violation of PRIMARY KEY constraint 'PK_dbo.ProductInventories'. Cannot insert duplicate key in object 'dbo.ProductInventories'. The duplicate key value is ({0})",
                             productName));
            }
        }

        [Test]
        public void When_using_a_custom_DbContext_then_it_is_available_in_UnitOfWork_resources()
        {
            var projector = new Projector<Order.CreditCardCharged>(() => new ReadModels1DbContext())
            {
                OnUpdate = (work, charged) => { work.Resource<ReadModels1DbContext>().Should().NotBeNull(); }
            };

            projector.UpdateProjection(new Order.CreditCardCharged
            {
                Amount = Any.PositiveInt()
            });
        }

        [Test]
        public void When_two_projectors_have_the_same_name_then_the_catchup_throws_on_creation()
        {
            var projector1 = Projector.Create<Order.Cancelled>(e => { });
            var projector2 = Projector.Create<Order.Cancelled>(e => { });
            Action create = () => CreateReadModelCatchup(projector1, projector2);

            create.ShouldThrow<ArgumentException>()
                  .And
                  .Message.Should().Contain(string.Format("Duplicate read model names:\n{0}",
                                                          EventHandler.FullName(projector1)));
        }

        [Test]
        public void Run_returns_CatchupRanAndHandledNewEvents_if_the_catchup_was_not_currently_running()
        {
            Events.Write(5);

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e => { })))
            {
                catchup.Run().Should().Be(ReadModelCatchupResult.CatchupRanAndHandledNewEvents);
            }
        }

        [Test]
        public void Run_returns_CatchupRanAndHandledNewEvents_if_the_catchup_was_not_currently_running_and_there_were_no_events()
        {
            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e => { })))
            {
                catchup.Run().Should().Be(ReadModelCatchupResult.CatchupRanButNoNewEvents);
            }
        }

        [Test]
        public void Run_returns_CatchupAlreadyInProgress_if_the_catchup_was_currently_running()
        {
            Events.Write(1);
            var barrier = new Barrier(2);

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e =>
            {
                barrier.SignalAndWait(1000);
            })))
            {
                Task.Run(() => catchup.Run());
                barrier.SignalAndWait(1000);
                catchup.Run().Should().Be(ReadModelCatchupResult.CatchupAlreadyInProgress);  
            }
        }

        [Test]
        public async Task When_Progress_is_awaited_then_it_completes_when_the_catchup_is_disposed()
        {
            Events.Write(5);
            long lastEventId = 0;

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e => { })))
            {
                catchup.Progress.Subscribe(s =>
                {
                    Console.WriteLine(s);
                    lastEventId = s.CurrentEventId;
                });

                Task.Run(() =>
                {
                    catchup.Run();
                    catchup.Dispose();
                });

                await catchup.Progress;
            }

            lastEventId.Should().Be(HighestEventId + 5);
        }

        [Test]
        public async Task SingleBatchAsync_can_be_used_to_observe_the_status_during_a_single_catchup_batch()
        {
            Events.Write(5);

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e => { })))
            {
                var statuses = await catchup.SingleBatchAsync().ToArray();
                
                var ids = statuses.Select(s => s.CurrentEventId);
                
                Console.WriteLine(new { ids }.ToLogString());
                
                ids.ShouldBeEquivalentTo(new[]
                {
                    HighestEventId + 1,
                    HighestEventId + 1,
                    HighestEventId + 2,
                    HighestEventId + 3,
                    HighestEventId + 4,
                    HighestEventId + 5
                });
            }
        }

        [Test]
        public async Task A_non_running_catchup_can_be_run_by_awaiting_SingleBatchAsync()
        {
            Events.Write(10);

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e => { })))
            {
                var status = await catchup.SingleBatchAsync();

                Console.WriteLine(status);
                status.IsEndOfBatch.Should().BeTrue();
                status.BatchCount.Should().Be(10);
                status.CurrentEventId.Should().Be(HighestEventId + 10);
            }
        }

        [Test]
        public async Task A_single_catchup_batch_can_be_triggered_and_awaited_using_SingleBatchAsync()
        {
            Events.Write(10);

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e => { })))
            {
                var status = await catchup.SingleBatchAsync().Do(s => Console.WriteLine(s));

                status.IsEndOfBatch.Should().BeTrue();
                status.BatchCount.Should().Be(10);
                status.CurrentEventId.Should().Be(HighestEventId + 10);
            }
        }

        [Test]
        public async Task The_current_batch_in_progress_can_be_awaited_using_SingleBatchAsync()
        {
            Events.Write(10);

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e =>
            {
                // slow this down just enough for the batch to still be running when we await below
                Thread.Sleep(1000);
            })))
            {
                Task.Run(() => catchup.Run());
                Thread.Sleep(1000);
                var status = await catchup.SingleBatchAsync();

                status.IsEndOfBatch.Should().BeTrue();
                status.BatchCount.Should().Be(10);
                status.CurrentEventId.Should().Be(HighestEventId + 10);
            }
        }

        [Test]
        public void When_a_Progress_subscriber_throws_then_catchup_continues()
        {
            Events.Write(10);
            int projectedEventCount = 0;

            using (var catchup = CreateReadModelCatchup(Projector.Create<IEvent>(e =>
            {
                projectedEventCount++;
            })))
            {
                catchup.Progress.Subscribe(e =>
                {
                    throw new Exception("oops!");
                });

                catchup.Run();
            }

            projectedEventCount.Should().Be(10);
        }

        [Test]
        public async Task Two_different_projectors_can_catch_up_to_two_different_event_stores_using_separate_catchups()
        {
            // arrange
            var projector1CallCount = 0;
            var projector2CallCount = 0;
            var projector1 = Projector.Create<Order.ItemAdded>(e => projector1CallCount++).Named(MethodBase.GetCurrentMethod().Name + "1");
            var projector2 = Projector.Create<Order.ItemAdded>(e => projector2CallCount++).Named(MethodBase.GetCurrentMethod().Name + "2");
            var startProjector2AtId = new OtherEventStoreDbContext().DisposeAfter(db => GetHighestEventId(db)) + 1;

            Events.Write(5, createEventStore: () => new EventStoreDbContext());
            Events.Write(5, createEventStore: () => new OtherEventStoreDbContext());

            using (
                var eventStoreCatchup = new ReadModelCatchup(projector1)
                {
                    StartAtEventId = HighestEventId + 1,
                    Name = "eventStoreCatchup",
                    CreateEventStoreDbContext = () => new EventStoreDbContext()
                })
            using (
                var otherEventStoreCatchup = new ReadModelCatchup(projector2)
                {
                    StartAtEventId = startProjector2AtId,
                    Name = "otherEventStoreCatchup",
                    CreateEventStoreDbContext = () => new OtherEventStoreDbContext()
                })
            {
                // act
                await eventStoreCatchup.SingleBatchAsync();
                await otherEventStoreCatchup.SingleBatchAsync();
            }

            // assert
            projector1CallCount.Should().Be(5, "projector1 should get all events from event stream");
            projector2CallCount.Should().Be(5, "projector2 should get all events from event stream");
        }

        public class Projector1 : IUpdateProjectionWhen<Order.ItemAdded>
        {
            public int CallCount { get; set; }

            public void UpdateProjection(Order.ItemAdded @event)
            {
                using (var work = this.Update())
                {
                    CallCount++;
                    OnUpdate(work, @event);
                    work.VoteCommit();
                }
            }

            public Action<UnitOfWork<ReadModelUpdate>, Order.ItemAdded> OnUpdate = (work, e) => { };
        }

        public class Projector2 : IUpdateProjectionWhen<Order.ItemAdded>
        {
            public int CallCount { get; set; }

            public void UpdateProjection(Order.ItemAdded @event)
            {
                using (var work = this.Update())
                {
                    CallCount++;
                    OnUpdate(work, @event);
                    work.VoteCommit();
                }
            }

            public Action<UnitOfWork<ReadModelUpdate>, Order.ItemAdded> OnUpdate = (work, e) => { };
        }
    }
}