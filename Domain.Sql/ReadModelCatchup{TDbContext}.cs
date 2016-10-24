// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Unit = System.Reactive.Unit;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Updates read models based on events after they have been added to an event store.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context where read models are to be updated.</typeparam>
    public class ReadModelCatchup<TDbContext> : IDisposable
        where TDbContext : DbContext
    {
        private readonly List<object> projectors;
        private readonly CompositeDisposable disposables;
        private MatchEvent[] matchEvents;

        private readonly Func<DbContext> createReadModelDbContext;
        private readonly Func<EventStoreDbContext> createEventStoreDbContext;

        private readonly CancellationDisposable cancellationDisposable;
        private readonly InProcessEventBus bus;
        private readonly ISubject<ReadModelCatchupStatus> progress = new Subject<ReadModelCatchupStatus>();
        private int running;
        private List<ReadModelInfo> unsubscribedReadModelInfos;
        private List<ReadModelInfo> subscribedReadModelInfos;
        private string lockResourceName;
        private readonly long startAtEventId;
        private readonly Expression<Func<StorableEvent, bool>> filter;
        private readonly int batchSize;
        private long eventStoreEventCount;
        private bool isInitialized;

        /// <summary>
        /// Initializes the <see cref="ReadModelCatchup{TDbContext}"/> class.
        /// </summary>
        static ReadModelCatchup()
        {
            ReadModelUpdate.ConfigureUnitOfWork();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelCatchup{TDbContext}" /> class.
        /// </summary>
        /// <param name="readModelDbContext">A delegate to create read model database contexts on demand.</param>
        /// <param name="eventStoreDbContext">A delegate to create event store database contexts on demand.</param>
        /// <param name="startAtEventId">The event id that the catchup should start from.</param>
        /// <param name="batchSize">The number of events queried from the event store at each iteration.</param>
        /// <param name="filter">An optional filter expression to constrain the query that the catchup uses over the event store.</param>
        /// <param name="projectors">The projectors to be updated as new events are added to the event store.</param>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        /// <exception cref="System.ArgumentException">You must specify at least one projector.</exception>
        public ReadModelCatchup(
            Func<DbContext> readModelDbContext,
            Func<EventStoreDbContext> eventStoreDbContext,
            long startAtEventId = 0,
            int batchSize = 10000,
            Expression<Func<StorableEvent, bool>> filter = null,
            params object[] projectors)
        {
            if (readModelDbContext == null)
            {
                throw new ArgumentNullException(nameof(readModelDbContext));
            }

            if (eventStoreDbContext == null)
            {
                throw new ArgumentNullException(nameof(eventStoreDbContext));
            }

            if (!projectors.OrEmpty().Any())
            {
                throw new ArgumentException("You must specify at least one projector.");
            }

            createReadModelDbContext = readModelDbContext;
            createEventStoreDbContext = eventStoreDbContext;
            this.startAtEventId = startAtEventId;
            this.filter = filter;
            this.projectors = new List<object>(projectors);
            this.batchSize = batchSize;

            EnsureProjectorNamesAreDistinct();

            cancellationDisposable = new CancellationDisposable();
            disposables = new CompositeDisposable
            {
                cancellationDisposable,
                Disposable.Create(() => progress.OnCompleted())
            };
            bus = new InProcessEventBus(new Subject<IEvent>());
            disposables.Add(bus.ReportErrorsToDatabase(() => createReadModelDbContext()));
            disposables.Add(bus);
            Sensors.ReadModelDbContexts.GetOrAdd(typeof (TDbContext).Name, createReadModelDbContext);   
        }

        /// <summary>
        /// Gets the event bus used to publish events to the subscribed projectors.
        /// </summary>
        public IEventBus EventBus => bus;

        /// <summary>
        /// Gets the event handlers that the catchup is running
        /// </summary>
        public IEnumerable<object> EventHandlers => projectors.ToArray();

        /// <summary>
        /// Gets or sets the name of the catchup. 
        /// </summary>
        /// <remarks>Catchups having the same name and updating the same database will not run in parallel. In order to have catchups that run in parallel for the same database, they should be given different names.</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets an observable sequence showing the catchup's progress.
        /// </summary>
        public IObservable<ReadModelCatchupStatus> Progress => progress;

        /// <summary>
        /// Runs a single catchup operation, which will catch up the subscribed projectors through the latest recorded event.
        /// </summary>
        /// <remarks>This method will return immediately without performing any updates if another catchup is currently in progress for the same read model database.</remarks>
        public async Task<ReadModelCatchupResult> Run()
        {
            if (disposables.IsDisposed)
            {
                throw new ObjectDisposedException($"The catchup has been disposed. ({this})");
            }

            // perform a re-entrancy check so that multiple catchups do not try to run concurrently
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                Debug.WriteLine($"Catchup {Name}: ReadModelCatchup already in progress. Skipping.", ToString());
                return ReadModelCatchupResult.CatchupAlreadyInProgress;
            }

            EnsureInitialized();

            long eventsProcessed = 0;
            var stopwatch = new Stopwatch();

            // iterate over the events in order
            try
            {
                using (var query = new ExclusiveEventStoreCatchupQuery(
                    await CreateOpenEventStoreDbContext(),
                    lockResourceName,
                    GetStartingId,
                    matchEvents,
                    batchSize,
                    filter))
                {
                    ReportStatus(new ReadModelCatchupStatus
                    {
                        BatchCount = query.ExpectedNumberOfEvents,
                        NumberOfEventsProcessed = eventsProcessed,
                        CurrentEventId = query.StartAtId,
                        CatchupName = Name
                    });

                    Debug.WriteLine(new { query });

                    if (query.ExpectedNumberOfEvents == 0)
                    {
                        return ReadModelCatchupResult.CatchupRanButNoNewEvents;
                    }

                    Debug.WriteLine($"Catchup {Name}: Beginning replay of {query.ExpectedNumberOfEvents} events");

                    stopwatch.Start();

                    eventsProcessed = await StreamEventsToProjections(query);
                }
            }
            catch (Exception exception)
            {
                // TODO: (Run) this should probably throw
                Trace.WriteLine($"Catchup {Name}: Read model catchup failed after {stopwatch.ElapsedMilliseconds}ms at {eventsProcessed} events.\n{exception}");
            }
            finally
            {
                // reset the re-entrancy flag
                running = 0;
                Debug.WriteLine($"Catchup {Name}: Catchup batch done.");
            }

            stopwatch.Stop();

            if (eventsProcessed > 0)
            {
                Debug.WriteLine(
                    $"Catchup {Name}: {eventsProcessed} events projected in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds/eventsProcessed}ms/event)");
            }

            return ReadModelCatchupResult.CatchupRanAndHandledNewEvents;
        }

        private async Task<long> StreamEventsToProjections(ExclusiveEventStoreCatchupQuery query)
        {
            long eventsProcessed = 0;

            foreach (var storedEvent in query.Events)
            {
                eventsProcessed++;

                IncludeReadModelsNeeding(storedEvent);

                if (cancellationDisposable.IsDisposed)
                {
                    break;
                }

                IEvent @event = null;
                var now = Clock.Now();

                try
                {
                    // update projectors
                    @event = storedEvent.ToDomainEvent();

                    if (@event != null)
                    {
                        using (var work = CreateUnitOfWork(@event))
                        {
                            await bus.PublishAsync(@event);
                             
                            var infos = work.Resource<DbContext>().Set<ReadModelInfo>();
                            
                            subscribedReadModelInfos.ForEach(i =>
                            {
                                var eventsRemaining = query.ExpectedNumberOfEvents - eventsProcessed;
                                
                                infos.Attach(i);
                                i.LastUpdated = now;
                                i.CurrentAsOfEventId = storedEvent.Id;
                                i.LatencyInMilliseconds = (now - @event.Timestamp).TotalMilliseconds;
                                i.BatchRemainingEvents = eventsRemaining;
                                
                                if (eventsProcessed == 1)
                                {
                                    i.BatchStartTime = now;
                                    i.BatchTotalEvents = query.ExpectedNumberOfEvents;
                                }
                                
                                if (i.InitialCatchupStartTime == null)
                                {
                                    i.InitialCatchupStartTime = now;
                                    i.InitialCatchupEvents = eventStoreEventCount;
                                }
                                
                                if (eventsRemaining == 0 && i.InitialCatchupEndTime == null)
                                {
                                    i.InitialCatchupEndTime = now;
                                }
                            });

                            work.VoteCommit();
                        }
                    }
                    else
                    {
                        throw new SerializationException($"Deserialization: Event type '{storedEvent.StreamName}.{storedEvent.Type}' not found");
                    }
                }
                catch (Exception ex)
                {
                    var error = @event == null
                                    ? SerializationError(ex, storedEvent)
                                    : new Domain.EventHandlingError(ex, @event: @event);

                    ReadModelUpdate.ReportFailure(
                        error,
                        createReadModelDbContext);
                }

                var status = new ReadModelCatchupStatus
                {
                    BatchCount = query.ExpectedNumberOfEvents,
                    NumberOfEventsProcessed = eventsProcessed,
                    CurrentEventId = storedEvent.Id,
                    EventTimestamp = storedEvent.Timestamp,
                    StatusTimeStamp = now,
                    CatchupName = Name
                };

                if (status.IsEndOfBatch)
                {
                    // reset the re-entrancy flag 
                    running = 0;
                    query.Dispose();
                }

                ReportStatus(status);
            }
            return eventsProcessed;
        }

        private void IncludeReadModelsNeeding(StorableEvent storedEvent)
        {
            if (unsubscribedReadModelInfos.Count > 0)
            {
                foreach (var readmodelInfo in unsubscribedReadModelInfos.ToArray())
                {
                    if (storedEvent.Id >= readmodelInfo.CurrentAsOfEventId + 1)
                    {
                        var handler = projectors.Single(p => ReadModelInfo.NameForProjector(p) == readmodelInfo.Name);
                        disposables.Add(bus.Subscribe(handler));
                        unsubscribedReadModelInfos.Remove(readmodelInfo);
                        subscribedReadModelInfos.Add(readmodelInfo);
                    }
                }
            }
        }

        private void ReportStatus(ReadModelCatchupStatus status)
        {
            try
            {
                progress.OnNext(status);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Catchup {Name}: Exception while reporting status @ {status}: {exception}");
            }
        }

        internal async Task<EventStoreDbContext> CreateOpenEventStoreDbContext()
        {
            var context = createEventStoreDbContext();
            await context.OpenAsync();
            return context;
        }

        private long GetStartingId()
        {
            var readModelInfos = subscribedReadModelInfos.Concat(unsubscribedReadModelInfos).ToArray();

            using (var db = createReadModelDbContext())
            {
                foreach (var readModelInfo in readModelInfos)
                {
                    db.Set<ReadModelInfo>().Attach(readModelInfo);
                }

                db.ChangeTracker.Entries<ReadModelInfo>().ForEach(e => e.Reload());
            }

            var existingReadModelInfosCount = readModelInfos.Length;
            long startAtId = 0;

            if (GetProjectorNames().Length == existingReadModelInfosCount)
            {
                // if all of the read models have been previously updated, we don't have to start at event 0
                startAtId = readModelInfos.Min(i => i.CurrentAsOfEventId) + 1;
            }

            return Math.Max(startAtId, startAtEventId);
        }

        private static Domain.EventHandlingError SerializationError(Exception ex, StorableEvent e)
        {
            var error = new EventHandlingDeserializationError(
                ex,
                e.Body,
                e.AggregateId,
                e.SequenceNumber,
                e.Timestamp,
                e.Actor,
                e.StreamName,
                e.Type);
            error.Metadata.AbsoluteSequenceNumber = e.Id;
            return error;
        }

        private UnitOfWork<ReadModelUpdate> CreateUnitOfWork(
            IEvent @event)
        {
            return new UnitOfWork<ReadModelUpdate>()
                .AddResource(@event)
                .EnsureDbContextIsInitialized(() => createReadModelDbContext());
        }

        /// <summary>
        /// Runs a single catchup operation each time the source observable produces a queryable of events.
        /// </summary>
        /// <param name="events">The events.</param>
        public void RunWhen(IObservable<Unit> events) =>
            disposables.Add(
                events
                    .Subscribe(
                        onNext: es =>
                        {
                            try
                            {
                                Run().Wait();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex);
                            }
                        },
                        onError: ex =>
                        { Debug.WriteLine(ex); }));

        private void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            try
            {
                InitializeMatchEvents();
                InitializeReadModelInfo();
                isInitialized = true;
            }
            catch (Exception ex)
            {
                EventBus.PublishErrorAsync(new Domain.EventHandlingError(ex));
            }
        }

        private void InitializeMatchEvents()
        {
            matchEvents = projectors.SelectMany(p => p.MatchesEvents())
                                    .Distinct()
                                    .Select(m => m.Type == "Scheduled"
                                                     ? new MatchEvent(m.StreamName, "*")
                                                     : m)
                                    .ToArray();

            Debug.WriteLine($"Catchup {Name}: Subscribing to event types: {matchEvents.Select(m => m.ToString()).ToJson()}");
        }

        private void InitializeReadModelInfo()
        {
            using (var db = createReadModelDbContext())
            {
                EnsureLockResourceNameIsInitialized(db);

                var existingReadModelInfoNames = GetProjectorNames();
                var existingReadModelInfos = db.Set<ReadModelInfo>()
                                               .OrderBy(i => i.Name)
                                               .Where(i => existingReadModelInfoNames.Contains(i.Name))
                                               .ToList();
                unsubscribedReadModelInfos = new List<ReadModelInfo>(existingReadModelInfos);
                subscribedReadModelInfos = new List<ReadModelInfo>();

                // create ReadModelInfo entries for any projectors that don't already have them
                foreach (var projector in projectors.Where(p => !unsubscribedReadModelInfos.Select(i => i.Name).Contains(ReadModelInfo.NameForProjector(p))))
                {
                    var readModelInfo = new ReadModelInfo
                    {
                        Name = ReadModelInfo.NameForProjector(projector)
                    };
                    db.Set<ReadModelInfo>().Add(readModelInfo);
                    unsubscribedReadModelInfos.Add(readModelInfo);
                }
                db.SaveChanges();
            }

            using (var eventStore = createEventStoreDbContext())
            {
                eventStoreEventCount = eventStore.Events.Count();
            }
        }

        private void EnsureLockResourceNameIsInitialized(DbContext db)
        {
            if (string.IsNullOrEmpty(lockResourceName))
            {
                lockResourceName = db.Database.Connection.Database +
                                   Name.IfNotNullOrEmptyOrWhitespace()
                                       .Then(n => ":" + n)
                                       .ElseDefault();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => disposables.Dispose();

        private void EnsureProjectorNamesAreDistinct()
        {
            var names = GetProjectorNames();
            if (names.Distinct().Count() != names.Length)
            {
                throw new ArgumentException("Duplicate read model names:\n" +
                                            names.Where(n => names.Count(nn => nn == n) > 1)
                                                 .Distinct()
                                                 .ToDelimitedString("\n"));
            }
        }

        private string[] GetProjectorNames() =>
            projectors
                .Select(ReadModelInfo.NameForProjector)
                .OrderBy(name => name)
                .ToArray();
    }
}
