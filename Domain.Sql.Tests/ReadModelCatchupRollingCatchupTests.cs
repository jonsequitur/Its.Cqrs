// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Its.Log.Instrumentation;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using Test.Domain.Ordering;
using Test.Domain.Ordering.Projections;
using Assert = NUnit.Framework.Assert;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;
using Unit = System.Reactive.Unit;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Catchups")]
    [TestFixture]
    public class RollingCatchupTest : EventStoreDbTest
    {
        [Test]
        public void Events_committed_to_the_event_store_are_caught_up_by_multiple_independent_read_model_stores()
        {
            var productName = Any.Paragraph(4);

            var projector1 = new Projector<Order.ItemAdded>(() => new ReadModels1DbContext())
            {
                OnUpdate = (work, e) => new ReadModels1DbContext().DisposeAfter(db => UpdateReservedInventory(db, e))
            };
            var projector2 = new Projector<Order.ItemAdded>(() => new ReadModels2DbContext())
            {
                OnUpdate = (work, e) => new ReadModels2DbContext().DisposeAfter(db => UpdateReservedInventory(db, e))
            };
            var numberOfEvents = Any.Int(10, 50);

            using (var disposables = new CompositeDisposable())
            using (var catchup1 = CreateReadModelCatchup<ReadModels1DbContext>(projector1))
            using (var catchup2 = CreateReadModelCatchup<ReadModels2DbContext>(projector2))
            {
                catchup1.Progress.ForEachAsync(p => Console.WriteLine("catchup1: " + p));
                catchup2.Progress.ForEachAsync(p => Console.WriteLine("catchup2: " + p));

                Action<string, ThreadStart> startThread =
                    (name, start) =>
                    {
                        var thread = new Thread(() =>
                        {
                            Console.WriteLine($"starting thread ({Thread.CurrentThread.ManagedThreadId})");
                            start();
                            Console.WriteLine($"ended thread ({Thread.CurrentThread.ManagedThreadId})");
                        });
                        thread.Name = name;
                        thread.Start();
                        disposables.Add(Disposable.Create(thread.Abort));
                    };

                Events.Write(numberOfEvents, i => new Order.ItemAdded
                {
                    ProductName = productName,
                    Quantity = 1,
                    AggregateId = Any.Guid()
                });
               
                startThread("catchup1", () =>
                {
                    catchup1.Run().TimeoutAfter(DefaultTimeout).Wait();
                    catchup1.Dispose();
                });
                startThread("catchup2", () =>
                {
                    catchup2.Run().TimeoutAfter(DefaultTimeout).Wait();
                    catchup2.Dispose();
                });

                Console.WriteLine("Waiting on catchups to complete");

                // wait on both catchups to complete
                catchup1
                    .Progress
                    .Timeout(DefaultTimeout)
                    .Merge(catchup2.Progress
                                   .Timeout(DefaultTimeout))
                    .Where(p => p.IsEndOfBatch)
                    .Take(2)
                    .Timeout(DefaultTimeout)
                    .Wait();
            }

            Action<DbContext> verify = db =>
            {
                var readModelInfoName = ReadModelInfo.NameForProjector(projector1);

                var readModelInfos = db.Set<ReadModelInfo>();
                Console.WriteLine(new { readModelInfos }.ToLogString());
                readModelInfos
                    .Single(i => i.Name == readModelInfoName)
                    .CurrentAsOfEventId
                    .Should()
                    .Be(HighestEventId + numberOfEvents);

                var productInventories = db.Set<ProductInventory>();
                Console.WriteLine(new { productInventories }.ToLogString());
                productInventories
                    .Single(pi => pi.ProductName == productName)
                    .QuantityReserved
                    .Should()
                    .Be(numberOfEvents);
            };

            Console.WriteLine("verifying ReadModels1DbContext...");
            new ReadModels1DbContext().DisposeAfter(r => verify(r));

            Console.WriteLine("verifying ReadModels2DbContext...");
            new ReadModels2DbContext().DisposeAfter(r => verify(r));
        }

        [Test]
        public async Task Rolling_catchup_can_be_run_based_on_event_store_polling()
        {
            var numberOfEvents = 50;
            Console.WriteLine("writing " + numberOfEvents + " starting at " + HighestEventId);

            // start the catchup in polling mode
            Projector<Order.ItemAdded> projector = null;
            var reading = Task.Run(() =>
            {
                projector = new Projector<Order.ItemAdded>(() => new ReadModels1DbContext());
                using (var catchup = CreateReadModelCatchup<ReadModels1DbContext>(projector).PollEventStore())
                {
                    catchup.Progress
                           .Do(s => Console.WriteLine(s))
                           .FirstAsync(s => s.IsEndOfBatch && s.CurrentEventId == numberOfEvents + HighestEventId)
                           .Timeout(DefaultTimeout)
                           .Wait();
                }
            });

            // now start writing a bunch of new events
            var writing = Task.Run(() => Enumerable.Range(1, numberOfEvents).ForEach(_ =>
            {
                // add a little variation into the intervals at which new events are written
                Thread.Sleep(Any.PositiveInt(500));
                Events.Write(1);
            }));

            await Task.WhenAll(writing, reading);

            using (var db = new ReadModels1DbContext())
            {
                var readModelInfoName = ReadModelInfo.NameForProjector(projector);
                db.Set<ReadModelInfo>()
                  .Single(i => i.Name == readModelInfoName)
                  .CurrentAsOfEventId
                  .Should()
                  .Be(HighestEventId + numberOfEvents);
            }
        }

        [Test]
        public void EventStore_polling_polls_again_immediately_if_new_events_were_written_while_the_previous_batch_was_processing()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Console.WriteLine(args.Exception.ToLogString());
            };

            Events.Write(1);
            var writeAdditionalEvent = true;
            var scheduler = new TestScheduler();
            var projector = new Projector<Order.ItemAdded>(() => new ReadModels1DbContext())
            {
                OnUpdate = (work, e) =>
                {
                    if (writeAdditionalEvent)
                    {
                        writeAdditionalEvent = false;
                        Events.Write(1);
                    }
                }
            };

            var statusReports = new List<ReadModelCatchupStatus>();
            using (var catchup = CreateReadModelCatchup<ReadModels1DbContext>(projector))
            using (catchup.Progress.Subscribe(s =>
            {
                statusReports.Add(s);
            }))
            {
                catchup.PollEventStore(TimeSpan.FromSeconds(30), scheduler);

                scheduler.AdvanceBy(TimeSpan.FromSeconds(30).Ticks);

                statusReports.Count(s => s.IsStartOfBatch)
                             .Should()
                             .Be(2);
            }
        }

        [Test]
        public async Task EventStore_polling_waits_for_specified_interval_if_no_new_events_have_been_written()
        {
            Events.Write(1);
            var projector = new Projector<Order.Shipped>(() => new ReadModels1DbContext());
            var statusReports = new List<ReadModelCatchupStatus>();
            var scheduler = new TestScheduler();
            using (var catchup = CreateReadModelCatchup<ReadModels1DbContext>(projector))
            {
                // catch up to the event store
                await catchup.Run();

                catchup.Progress
                       .ForEachAsync(s =>
                       {
                           statusReports.Add(s);
                           Console.WriteLine(s);
                       });

                // act
                catchup.PollEventStore(TimeSpan.FromSeconds(30), scheduler);

                Console.WriteLine("start polling");
                scheduler.AdvanceBy(TimeSpan.FromSeconds(59).Ticks);
            }

            // assert
            statusReports.Count.Should().Be(1);
        }

        [Test]
        public void EventStore_polling_continues_even_if_connection_gets_closed_during_replay()
        {
            Events.Write(3);
            var scheduler = new TestScheduler();

            var projector = new Projector<Order.ItemAdded>(() => new ReadModels1DbContext())
            {
                OnUpdate = (work, e) => { }
            };

            var statusReports = new List<ReadModelCatchupStatus>();
            var catchup = CreateReadModelCatchup<ReadModels1DbContext>(projector);

            DbConnection dbConnection = new SqlConnection();
            Configuration.Current.UseDependency(_ => 
            {
                dbConnection = ((IObjectContextAdapter) EventStoreDbContext()).ObjectContext.Connection;
                return EventStoreDbContext();
            });

            using (catchup)
            {
                catchup.Progress
                       .ForEachAsync(s =>
                       {
                           Console.WriteLine(s);
                           if (!s.IsStartOfBatch && !s.IsEndOfBatch)
                           {
                               Console.WriteLine("closing the connection");
                               // close the connection                                                              
                               dbConnection.Close();
                           }
                           statusReports.Add(s);
                       });

                catchup.PollEventStore(TimeSpan.FromSeconds(5), scheduler);

                // Advance to trigger the first catchup
                scheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);

                // Trigger an empty batch replay
                scheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);

                Events.Write(2);

                // Advance to trigger the polling catchup
                scheduler.AdvanceBy(TimeSpan.FromSeconds(9).Ticks);

                statusReports.Count(s => s.IsStartOfBatch)
                             .Should()
                             .Be(3);
            }
        }

        [Test]
        public void EventStore_polling_replays_new_events_inserted_after_previous_catchup_completed()
        {
            Events.Write(3);
            var scheduler = new TestScheduler();

            var projector = new Projector<Order.ItemAdded>(() => new ReadModels1DbContext())
            {
                OnUpdate = (work, e) => { }
            };

            var statusReports = new List<ReadModelCatchupStatus>();
            using (var catchup = CreateReadModelCatchup<ReadModels1DbContext>(projector))
            {
                catchup.Progress
                       .ForEachAsync(s =>
                       {
                           statusReports.Add(s);
                           Console.WriteLine(s);
                       });

                catchup.PollEventStore(TimeSpan.FromSeconds(5), scheduler);

                // Advance to trigger the first catchup
                scheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);

                // Trigger an empty batch replay
                scheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);

                Events.Write(2);

                // Advance to trigger the polling catchup
                scheduler.AdvanceBy(TimeSpan.FromSeconds(9).Ticks);

                statusReports.Count(s => s.IsStartOfBatch)
                             .Should().Be(3);
            }
        }

        [Test]
        public void When_one_concurrent_catchup_instance_terminates_due_to_deliberate_disposal_then_another_tries_to_take_over_immediately()
        {
            // arrange
            int numberOfEventsToWrite = 15;
            Events.Write(numberOfEventsToWrite);
            var scheduler = new TestScheduler();

            var projector1 = new Projector<IEvent>(() => new ReadModels1DbContext());
            var projector2 = new Projector<IEvent>(() => new ReadModels1DbContext());
            var catchup1StatusReports = new List<ReadModelCatchupStatus>();
            var catchup2StatusReports = new List<ReadModelCatchupStatus>();

            using (var catchup1 = CreateReadModelCatchup<ReadModels1DbContext>(projector1))
            using (var catchup2 = CreateReadModelCatchup<ReadModels1DbContext>(projector2))
            {
                catchup1.Progress.ForEachAsync(s =>
                {
                    catchup1StatusReports.Add(s);
                    Console.WriteLine("catchup1: " + s);

                    // when we've processed at least one event, cancel this catchup
                    if (!s.IsStartOfBatch && s.NumberOfEventsProcessed > 0)
                    {
                        catchup1.Dispose();
                    }
                });
                catchup2.Progress.ForEachAsync(s =>
                {
                    catchup2StatusReports.Add(s);
                    Console.WriteLine("catchup2: " + s);
                });

                // act
                catchup1.PollEventStore(TimeSpan.FromSeconds(3), scheduler);

                scheduler.Schedule(TimeSpan.FromSeconds(.5),
                                   () =>
                                   {
                                       Console.WriteLine("scheduling catchup2 polling");
                                       catchup2.PollEventStore(TimeSpan.FromSeconds(3), scheduler);
                                   });
                scheduler.AdvanceBy(TimeSpan.FromSeconds(3.5).Ticks);
            }

            // assert
            catchup1StatusReports.Count(s => !s.IsStartOfBatch)
                                 .Should()
                                 .Be(1, "sanity check that catchup1 polled");
            catchup2StatusReports.Count(s => !s.IsStartOfBatch)
                                 .Should()
                                 .Be(numberOfEventsToWrite - 1);
            catchup2StatusReports.Count(s => s.IsStartOfBatch)
                                 .Should()
                                 .Be(1, "sanity check that catchup2 polled");

            var expected = Enumerable.Range((int) HighestEventId + 1, numberOfEventsToWrite);
            Console.WriteLine("expected: " + expected.ToLogString());
            var processed = catchup1StatusReports.Concat(catchup2StatusReports).Where(s => !s.IsStartOfBatch).Select(s => s.CurrentEventId).ToArray();
            Console.WriteLine("actual: " + processed.ToLogString());
            processed.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void When_one_concurrent_catchup_instance_terminates_due_to_eventstore_connection_loss_then_another_tries_to_take_over_immediately()
        {
            // arrange
            var numberOfEventsToWrite = 100;
            Events.Write(numberOfEventsToWrite);

            var projector1 = new Projector<IEvent>(() => new ReadModels1DbContext());
            var projector2 = new Projector<IEvent>(() => new ReadModels1DbContext());
            var catchup1StatusReports = new List<ReadModelCatchupStatus>();
            var catchup2StatusReports = new List<ReadModelCatchupStatus>();
       
            DbConnection dbConnection1 = null;

            using (var catchup1 = CreateReadModelCatchup<ReadModels1DbContext>(() =>
            {
                var eventStoreDbContext = EventStoreDbContext();
                dbConnection1 = ((IObjectContextAdapter) eventStoreDbContext).ObjectContext.Connection;
                return eventStoreDbContext;
            }, projector1))
            using (var catchup2 = CreateReadModelCatchup<ReadModels1DbContext>(projector2))
            {
                catchup1.Progress.ForEachAsync(s =>
                {
                    catchup1StatusReports.Add(s);
                    Console.WriteLine("catchup1: " + s);

                    // when we've processed one event, cancel this catchup
                    dbConnection1.Close();
                });
                catchup2.Progress.ForEachAsync(s =>
                {
                    catchup2StatusReports.Add(s);
                    Console.WriteLine("catchup2: " + s);
                });

                // act
                catchup1.PollEventStore(TimeSpan.FromSeconds(10), new NewThreadScheduler());

                new NewThreadScheduler().Schedule(TimeSpan.FromSeconds(5),
                                                  () =>
                                                  {
                                                      Console.WriteLine("scheduling catchup2 polling");
                                                      catchup2.PollEventStore(TimeSpan.FromSeconds(10), new NewThreadScheduler());
                                                  });

                var waitingOnEventId = HighestEventId + numberOfEventsToWrite;
                Console.WriteLine($"waiting on event id {waitingOnEventId} to be processed");
                catchup1.Progress
                        .Merge(catchup2.Progress)
                        .FirstAsync(s => s.CurrentEventId == waitingOnEventId)
                        .Timeout(DefaultTimeout)
                        .Wait();
            }

            // assert
            catchup1StatusReports.Count(s => !s.IsStartOfBatch)
                                 .Should()
                                 .Be(1, "sanity check that catchup1 polled");
            catchup2StatusReports.Count(s => !s.IsStartOfBatch)
                                 .Should()
                                 .Be(numberOfEventsToWrite - 1);
            catchup2StatusReports.Count(s => s.IsStartOfBatch)
                                 .Should()
                                 .Be(1, "sanity check that catchup2 polled");

            var expected = Enumerable.Range((int) HighestEventId + 1, numberOfEventsToWrite);
            Console.WriteLine("expected: " + expected.ToLogString());
            var processed = catchup1StatusReports.Concat(catchup2StatusReports).Where(s => !s.IsStartOfBatch).Select(s => s.CurrentEventId).ToArray();
            Console.WriteLine("actual: " + processed.ToLogString());
            processed.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void When_concurrent_catchups_are_all_caught_up_then_subsequent_events_are_processed_in_less_than_the_poll_time()
        {
            // arrange
            int numberOfEventsToWrite = 50;
            var lastEventId = Events.Write(numberOfEventsToWrite);

            var projector1 = new Projector<IEvent>(() => new ReadModels1DbContext());
            var projector2 = new Projector<IEvent>(() => new ReadModels1DbContext());

            using (var catchup1 = CreateReadModelCatchup<ReadModels1DbContext>(projector1))
            using (var catchup2 = CreateReadModelCatchup<ReadModels1DbContext>(projector2))
            {
                var catchup1Progress = catchup1.Progress;
                var catchup2Progress = catchup2.Progress;
                catchup1Progress.ForEachAsync(s =>
                {
                    Console.WriteLine("catchup1: " + s);
                });
                catchup2Progress.ForEachAsync(s =>
                {
                    Console.WriteLine("catchup2: " + s);
                });

                var pollInterval = TimeSpan.FromSeconds(1);
                catchup1.PollEventStore(pollInterval, new NewThreadScheduler());
                catchup2.PollEventStore(pollInterval, new NewThreadScheduler());

                catchup1Progress.Merge(catchup2Progress)
                                .FirstAsync(s => s.CurrentEventId == lastEventId)
                                .Timeout(DefaultTimeout)
                                .Wait();

                // act: new events should be caught up after a few idle polls
                Thread.Sleep(TimeSpan.FromSeconds(3));

                lastEventId = Events.Write(5);
                Console.WriteLine(new { lastEventId });

                catchup1Progress.Merge(catchup2Progress)
                                .Do(s =>
                                {
                                    if (s.Latency > pollInterval)
                                    {
                                        Assert.Fail(string.Format("Latency ({0}) exceeded poll interval {1}\n({2})",
                                                                  s.Latency.Value.TotalSeconds,
                                                                  pollInterval.TotalSeconds,
                                                                  s));
                                    }
                                })
                                .FirstAsync(s => s.CurrentEventId == lastEventId)
                                .Timeout(TimeSpan.FromSeconds(3))
                                .Wait();
            }
        }

        [Test]
        public void When_a_catchup_has_been_waiting_for_several_poll_intervals_it_only_runs_once()
        {
            // arrange
            int numberOfEventsToWrite = Any.Int(10, 20);
            Events.Write(numberOfEventsToWrite);
            var testScheduler = new TestScheduler();

            var projector1 = new Projector<IEvent>(() => new ReadModels1DbContext())
            {
                OnUpdate = (work, e) =>
                {
                    // create some delay so that catchup2 will attempt to poll multiple times
                    testScheduler.Sleep(1000);
                }
            };
            var projector2 = new Projector<IEvent>(() => new ReadModels1DbContext());
            var catchup2StatusReports = new List<ReadModelCatchupStatus>();

            using (var catchup1 = CreateReadModelCatchup<ReadModels1DbContext>(projector1))
            using (var catchup2 = CreateReadModelCatchup<ReadModels1DbContext>(projector2))
            {
                bool catchup1Disposed = false;
                catchup1.Progress.ForEachAsync(s =>
                {
                    Console.WriteLine("catchup1: " + s);

                    // when the batch is done, dispose, which should allow catchup2 to try
                    if (s.IsEndOfBatch)
                    {
                        Console.WriteLine("disposing catchup1");
                        catchup1.Dispose();
                        catchup1Disposed = true;
                    }
                });
                catchup2.Progress.ForEachAsync(s =>
                {
                    catchup2StatusReports.Add(s);
                    Console.WriteLine("catchup2: " + s);
                });

                // act
                var scheduler1 = new SchedulerWatcher(testScheduler, "scheduler1");
                catchup1.PollEventStore(TimeSpan.FromSeconds(1), scheduler1);

                var scheduler2 = new SchedulerWatcher(testScheduler, "scheduler2");
                scheduler2.Schedule(TimeSpan.FromSeconds(1.5),
                                    () =>
                                    {
                                        Console.WriteLine("catchup2 polling starting");
                                        // use a higher poll frequency so the poll timer fires many times while catchup1 is running
                                        catchup2.PollEventStore(TimeSpan.FromSeconds(.5), scheduler2);
                                    });

                while (!catchup1Disposed)
                {
                    testScheduler.AdvanceBy(TimeSpan.FromSeconds(.1).Ticks);
                }
                testScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
            }

            // assert
            catchup2StatusReports.Count(s => s.IsStartOfBatch)
                                 .Should()
                                 .Be(1);
        }

        [Test]
        public async Task Catchups_can_be_run_in_parallel_for_different_projectors()
        {
            var projector1Count = 0;
            var projector2Count = 0;
            var projector1 = Projector.Create<Order.CreditCardCharged>(e => projector1Count++);
            var projector2 = Projector.Create<Order.CustomerInfoChanged>(e => projector2Count++);

            Events.Write(15, i => new Order.CreditCardCharged());
            Events.Write(25, i => new Order.CustomerInfoChanged());
            var catchup1 = CreateReadModelCatchup(projector1);
            catchup1.Name = MethodBase.GetCurrentMethod().Name + "1";
            var catchup2 = CreateReadModelCatchup(projector2);
            catchup2.Name = MethodBase.GetCurrentMethod().Name + "2";

            catchup1.Progress.Subscribe(s => Console.WriteLine("catchup1: " + s));
            catchup2.Progress.Subscribe(s => Console.WriteLine("catchup2: " + s));

            using (catchup1.PollEventStore())
            using (catchup2.PollEventStore())
            {
                await Catchup.SingleBatchAsync(catchup1, catchup2);
            }

            projector1Count.Should().Be(15);
            projector2Count.Should().Be(25);
        }

        [Test]
        public async Task When_initialization_fails_then_polling_continues()
        {
            var shouldThrow = true;
            var eventsReceived = 0;
            Events.Write(15, i => new Order.CreditCardCharged());

            Func<EventStoreDbContext> getDbContext = () =>
            {
                if (shouldThrow)
                {
                    shouldThrow = false;
                    throw new Exception("oops!");
                }

                return Configuration.Current.EventStoreDbContext();
            };

            var subject = new Subject<Unit>();

            var catchup = CreateReadModelCatchup<ReadModels1DbContext>(
                    getDbContext,
                    Projector.Create<Order.CreditCardCharged>(e => eventsReceived++))
                .PollEventStore(subject);

            subject.OnNext(Unit.Default);
            subject.OnNext(Unit.Default);

            eventsReceived.Should().Be(15);
        }

        private static TimeSpan DefaultTimeout
        {
            get
            {
                if (!Debugger.IsAttached)
                {
                    return TimeSpan.FromSeconds(60);
                }

                return TimeSpan.FromMinutes(60);
            }
        }

        private static void UpdateReservedInventory(DbContext db, Order.ItemAdded e)
        {
            var inventoryRecord = db.Set<ProductInventory>()
                                    .SingleOrDefault(r => r.ProductName == e.ProductName)
                                    .IfNotNull()
                                    .Then(r => r)
                                    .Else(() =>
                                    {
                                        var r = new ProductInventory
                                        {
                                            ProductName = e.ProductName
                                        };
                                        db.Set<ProductInventory>().Add(r);
                                        return r;
                                    });

            inventoryRecord.QuantityReserved += e.Quantity;

            db.SaveChanges();
        }
    }

    public class SchedulerWatcher : IScheduler
    {
        private readonly IScheduler inner;
        private string name;

        public SchedulerWatcher(IScheduler innerScheduler, string name)
        {
            if (innerScheduler == null)
            {
                throw new ArgumentNullException("innerScheduler");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            this.name = name;
            inner = innerScheduler;
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            Console.WriteLine("> " + name + " scheduling: " + new { state, action }.ToLogString());
            return inner.Schedule(state, action);
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            Console.WriteLine("> " + name + " scheduling: " + new { state, dueTime, action }.ToLogString());
            return inner.Schedule(state, dueTime, action);
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            Console.WriteLine("> " + name + " scheduling: " + new { state, dueTime, action }.ToLogString());
            return inner.Schedule(state, dueTime, action);
        }

        public DateTimeOffset Now
        {
            get
            {
                return DateTime.Now;
            }
        }
    }
}
