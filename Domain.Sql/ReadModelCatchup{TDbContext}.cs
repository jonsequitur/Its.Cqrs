using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Unit = System.Reactive.Unit;
using log = Its.Log.Lite.Log;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Updates read models based on events after they have been added to an event store.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the database context where read models are to be updated.</typeparam>
    public class ReadModelCatchup<TDbContext> : IDisposable
        where TDbContext : DbContext, new()
    {
        private readonly List<object> projectors;
        private readonly CompositeDisposable disposables;
        private MatchEvent[] matchEvents;

        /// <summary>
        /// Provides a method for specifying how <see cref="DbContext" /> instances are created for use by instances of <see cref="ReadModelCatchup{TDbContext}" />. 
        /// </summary>
        public Func<DbContext> CreateReadModelDbContext = () => new TDbContext();

        /// <summary>
        /// Provides a method for specifying how the EventStoreDbContext instances are created for use by instance of <see cref="ReadModelCatchup{TBdContext}" />
        /// </summary>
        public Func<EventStoreDbContext> CreateEventStoreDbContext = () => new EventStoreDbContext();

        internal EventStoreDbContext CreateOpenEventStoreDbContext()
        {
            var context = CreateEventStoreDbContext();
            var dbConnection = ((IObjectContextAdapter) context).ObjectContext.Connection;
            dbConnection.Open();
            return context;
        }

        private readonly CancellationDisposable cancellationDisposable;
        private readonly InProcessEventBus bus;
        private readonly ISubject<ReadModelCatchupStatus> progress = new Subject<ReadModelCatchupStatus>();
        private int running;
        private List<ReadModelInfo> unsubscribedReadModelInfos;
        private List<ReadModelInfo> subscribedReadModelInfos;
        private string lockResourceName;

        /// <summary>
        /// Initializes the <see cref="ReadModelCatchup{TDbContext}"/> class.
        /// </summary>
        static ReadModelCatchup()
        {
            ReadModelUpdate.ConfigureUnitOfWork();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadModelCatchup{TDbContext}"/> class.
        /// </summary>
        /// <param name="projectors">The projectors to be updated as new events are added to the event store.</param>
        /// <exception cref="System.ArgumentException">You must specify at least one projector.</exception>
        public ReadModelCatchup(params object[] projectors)
        {
            if (!projectors.OrEmpty().Any())
            {
                throw new ArgumentException("You must specify at least one projector.");
            }

            this.projectors = new List<object>(projectors);

            EnsureProjectorNamesAreDistinct();

            cancellationDisposable = new CancellationDisposable();
            disposables = new CompositeDisposable
            {
                cancellationDisposable,
                Disposable.Create(() => progress.OnCompleted())
            };
            bus = new InProcessEventBus(new Subject<IEvent>());
            disposables.Add(bus.ReportErrorsToDatabase(() => CreateReadModelDbContext()));
            disposables.Add(bus);
            Sensors.ReadModelDbContexts.GetOrAdd(typeof (TDbContext).Name, CreateReadModelDbContext);
        }

        /// <summary>
        /// Gets the event bus used to publish events to the subscribed projectors.
        /// </summary>
        public IEventBus EventBus
        {
            get
            {
                return bus;
            }
        }

        /// <summary>
        /// Gets the event handlers that the catchup is running
        /// </summary>
        public IEnumerable<object> EventHandlers
        {
            get
            {
                return projectors.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the name of the catchup. 
        /// </summary>
        /// <remarks>Catchups having the same name and updating the same database will not run in parallel. In order to have catchups that run in parallel for the same database, they should be given different names.</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets an observable sequence showing the catchup's progress.
        /// </summary>
        public IObservable<ReadModelCatchupStatus> Progress
        {
            get
            {
                return progress;
            }
        }

        /// <summary>
        /// Specifies the lowest id of the events to be caught up.
        /// </summary>
        public long StartAtEventId { get; set; }

        /// <summary>
        /// Runs a single catchup operation, which will catch up the subscribed projectors through the latest recorded event.
        /// </summary>
        /// <remarks>This method will return immediately without performing any updates if another catchup is currently in progress for the same read model database.</remarks>
        public ReadModelCatchupResult Run()
        {
            // perform a re-entrancy check so that multiple catchups do not try to run concurrently
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0)
            {
                Debug.WriteLine(string.Format("Catchup {0}: ReadModelCatchup already in progress. Skipping.", Name), ToString());
                return ReadModelCatchupResult.CatchupAlreadyInProgress;
            }

            EnsureInitialized();

            long eventsProcessed = 0;
            var stopwatch = new Stopwatch();

            // iterate over the events in order
            try
            {
                using (var query = new ExclusiveEventStoreCatchupQuery(
                    CreateOpenEventStoreDbContext(),
                    lockResourceName,
                    GetStartingId,
                    matchEvents))
                {
                    ReportStatus(new ReadModelCatchupStatus
                    {
                        BatchCount = query.ExpectedNumberOfEvents,
                        NumberOfEventsProcessed = eventsProcessed,
                        CurrentEventId = query.StartAtId,
                        CatchupName = Name
                    });

                    if (query.ExpectedNumberOfEvents == 0)
                    {
                        return ReadModelCatchupResult.CatchupRanButNoNewEvents;
                    }

                    Debug.WriteLine(string.Format("Catchup {0}: Beginning replay of {1} events", Name, query.ExpectedNumberOfEvents));

                    stopwatch.Start();

                    eventsProcessed = StreamEventsToProjections(query);
                }
            }
            catch (Exception exception)
            {
                // TODO: (Run) this should probably throw
                Debug.WriteLine(string.Format("Catchup {0}: Read model catchup failed after {1}ms at {2} events.\n{3}", Name, stopwatch.ElapsedMilliseconds, eventsProcessed,
                                              exception));
            }
            finally
            {
                // reset the re-entrancy flag
                running = 0;
                Debug.WriteLine(string.Format("Catchup {0}: Catchup batch done.", Name));
            }

            stopwatch.Stop();

            if (eventsProcessed > 0)
            {
                Debug.WriteLine(
                    "Catchup {0}: {1} events projected in {2}ms ({3}ms/event)",
                    Name,
                    eventsProcessed,
                    stopwatch.ElapsedMilliseconds,
                    (stopwatch.ElapsedMilliseconds/eventsProcessed));
            }

            return ReadModelCatchupResult.CatchupRanAndHandledNewEvents;
        }

        private long StreamEventsToProjections(ExclusiveEventStoreCatchupQuery query)
        {
            long eventsProcessed = 0;

            foreach (var storedEvent in query.Events)
            {
                eventsProcessed++;

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
                            bus.PublishAsync(@event).Wait();
                             
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
                                    i.InitialCatchupEvents = query.ExpectedNumberOfEvents;
                                }
                                if (eventsRemaining == 0 & i.InitialCatchupEndTime == null)
                                {
                                    i.InitialCatchupEndTime = now;
                                }
                            });

                            work.VoteCommit();
                        }
                    }
                    else
                    {
                        throw new SerializationException(string.Format(
                            "Deserialization: Event type '{0}.{1}' not found",
                            storedEvent.StreamName,
                            storedEvent.Type));
                    }
                }
                catch (Exception ex)
                {
                    var error = @event == null
                                    ? SerializationError(ex, storedEvent)
                                    : new Domain.EventHandlingError(ex, @event: @event);

                    ReadModelUpdate.ReportFailure(
                        error,
                        () => CreateReadModelDbContext());
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

        private void ReportStatus(ReadModelCatchupStatus status)
        {
            try
            {
                progress.OnNext(status);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(string.Format("Catchup {0}: Exception while reporting status @ {1}: {2}", Name, status, exception));
            }
        }

        private long GetStartingId()
        {
            var readModelInfos = subscribedReadModelInfos.Concat(unsubscribedReadModelInfos).ToArray();
            using (var db = CreateReadModelDbContext())
            {
                foreach (var readModelInfo in readModelInfos)
                {
                    db.Set<ReadModelInfo>().Attach(readModelInfo);
                }

                db.ChangeTracker.Entries<ReadModelInfo>().ForEach(e => e.Reload());
            }

            var existingReadModelInfosCount = readModelInfos.Length;
            long startAtId = 0;
           
            if (GetProjectorNames().Count() == existingReadModelInfosCount)
            {
                // if all of the read models have been previously updated, we don't have to start at event 0
                startAtId = readModelInfos.Min(i => i.CurrentAsOfEventId) + 1;
            }

            return Math.Max(startAtId, StartAtEventId);
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
                .EnsureDbContextIsInitialized(() => CreateReadModelDbContext());
        }

        /// <summary>
        /// Runs a single catchup operation each time the source observable produces a queryable of events.
        /// </summary>
        /// <param name="events">The events.</param>
        public void RunWhen(IObservable<Unit> events)
        {
            disposables.Add(events.Subscribe(es => Run()));
        }

        private void EnsureInitialized()
        {
            if (subscribedReadModelInfos != null)
            {
                return;
            }

            // figure out which event types we will need to query
            matchEvents = projectors.SelectMany(p => p.MatchesEvents())
                                    .Distinct()
                                    .Select(m =>
                                    {
                                        // TODO: (EnsureInitialized) optimize this by figuring out how to query SteamName LIKE 'Scheduled:%'
                                        if (m.Type == "Scheduled")
                                        {
                                            return new MatchEvent(m.StreamName, "*");
                                        }
                                        return m;
                                    })
                                    .ToArray();

            Debug.WriteLine(string.Format("Catchup {0}: Subscribing to event types: {1}", Name, matchEvents.Select(m => m.ToString()).ToJson()));

            using (var db = CreateReadModelDbContext())
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
        public void Dispose()
        {
            disposables.Dispose();
        }

        private void EnsureProjectorNamesAreDistinct()
        {
            var names = GetProjectorNames();
            if (names.Distinct().Count() != names.Count())
            {
                throw new ArgumentException("Duplicate read model names:\n" +
                                            names.Where(n => names.Count(nn => nn == n) > 1)
                                                 .Distinct()
                                                 .ToDelimitedString("\n"));
            }
        }

        private string[] GetProjectorNames()
        {
            return projectors
                .Select(ReadModelInfo.NameForProjector)
                .OrderBy(name => name)
                .ToArray();
        }
    }
}