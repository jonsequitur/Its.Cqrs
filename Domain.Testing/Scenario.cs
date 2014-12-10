using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Represents a prepared test scenario.
    /// </summary>
    public class Scenario : IDisposable
    {
        internal readonly ScenarioBuilder builder;
        internal readonly HashSet<IEventSourced> aggregates = new HashSet<IEventSourced>(new EventSourcedComparer());
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly ConcurrentBag<EventHandlingError> eventHandlingErrors = new ConcurrentBag<EventHandlingError>();
        private bool subscribedToVirtualClock;
        private readonly string clockName;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public Scenario(ScenarioBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            this.builder = builder;

            disposables.Add(builder.EventBus.Errors.Subscribe(e => eventHandlingErrors.Add(e)));

            clockName = builder.Configuration
                               .Properties
                               .IfContains("CommandSchedulerClockName")
                               .And()
                               .IfTypeIs<string>()
                               .ElseDefault();

            Clock.Current
                 .IfTypeIs<VirtualClock>()
                 .ThenDo(c =>
                 {
                     subscribedToVirtualClock = true;
                     disposables.Add(c.Subscribe(onNext: t => AdvanceClock(t).Wait()));
                 });
        }

        /// <summary>
        /// Gets the aggregates that have been created within the scenario.
        /// </summary>
        public IEnumerable<IEventSourced> Aggregates
        {
            get
            {
                return aggregates;
            }
        }

        /// <summary>
        /// Gets the event bus on which handlers in the scenario are subscribed.
        /// </summary>
        public FakeEventBus EventBus
        {
            get
            {
                return builder.EventBus;
            }
        }

        /// <summary>
        /// Gets the event handling errors, if any, that occur during the course of the scenario, including during <see cref="ScenarioBuilder.Prepare" />.
        /// </summary>
        public IEnumerable<EventHandlingError> EventHandlingErrors
        {
            get
            {
                return eventHandlingErrors;
            }
        }

        internal void AddEventHandlingError(EventHandlingError error)
        {
            eventHandlingErrors.Add(error);
        }

        internal async Task AdvanceClock(TimeSpan byTimeSpan)
        {
            if (!subscribedToVirtualClock)
            {
                VirtualClock.Current.AdvanceBy(byTimeSpan);
            }

            if (!builder.useInMemoryCommandScheduling)
            {
                var scheduler = builder.Configuration.Container.Resolve<SqlCommandScheduler>();
                await scheduler.AdvanceClock(clockName, byTimeSpan);
            }
        }

        internal async Task AdvanceClock(DateTimeOffset to)
        {
            if (!subscribedToVirtualClock)
            {
                VirtualClock.Current.AdvanceTo(to);
            }

            if (!builder.useInMemoryCommandScheduling)
            {
                var scheduler = builder.Configuration.Container.Resolve<SqlCommandScheduler>();
                await scheduler.AdvanceClock(clockName, to);
            }
        }

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        public void Save<TAggregate>(TAggregate aggregate)
            where TAggregate : class, IEventSourced
        {
            aggregates.Add(aggregate);
            builder.GetRepository(aggregate.GetType()).Save(aggregate);
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate. If null, and there's only a single aggregate of the specified type, it returns that; otherwise, it throws.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetLatest<TAggregate>(Guid? aggregateId = null)
            where TAggregate : class, IEventSourced
        {
            if (!aggregateId.HasValue)
            {
                aggregateId = Aggregates.OfType<TAggregate>().Single().Id;
            }

            return builder.GetRepository(typeof (TAggregate)).GetLatest(aggregateId.Value);
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetVersion<TAggregate>(Guid aggregateId, long version)
            where TAggregate : class, IEventSourced
        {
            return builder.GetRepository(typeof (TAggregate)).GetVersion(aggregateId, version);
        }

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        public TAggregate GetAsOfDate<TAggregate>(Guid aggregateId, DateTimeOffset asOfDate)
            where TAggregate : class, IEventSourced
        {
            return builder.GetRepository(typeof (TAggregate)).GetAsOfDate(aggregateId, asOfDate);
        }

        /// <summary>
        /// Registers an object for disposal when the scenario is disposed.
        /// </summary>
        public void RegisterForDispose(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}