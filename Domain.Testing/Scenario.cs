// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public Scenario(ScenarioBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            this.builder = builder;

            disposables.Add(builder.EventBus.Errors.Subscribe(e => eventHandlingErrors.Add(e)));

            // subscribe to VirtualClock movements and advance the scheduler clock accordingly
            Clock.Current
                 .IfTypeIs<VirtualClock>()
                 .ThenDo(virtualClock => { subscribedToVirtualClock = true; });
        }

        /// <summary>
        /// Gets the aggregates that have been created within the scenario.
        /// </summary>
        public IEnumerable<IEventSourced> Aggregates => aggregates;

        /// <summary>
        /// Gets the event bus on which handlers in the scenario are subscribed.
        /// </summary>
        public FakeEventBus EventBus => builder.EventBus;

        /// <summary>
        /// Gets the event handling errors, if any, that occur during the course of the scenario, including during <see cref="ScenarioBuilder.Prepare" />.
        /// </summary>
        public IEnumerable<EventHandlingError> EventHandlingErrors => eventHandlingErrors;

        internal void AddEventHandlingError(EventHandlingError error) => eventHandlingErrors.Add(error);

        internal async Task AdvanceClock(DateTimeOffset to)
        {
            if (!subscribedToVirtualClock)
            {
                VirtualClock.Current.AdvanceTo(to);
            }

            if (!builder.Configuration.IsUsingInMemoryCommandScheduling())
            {
                var clockTrigger = builder.Configuration.SchedulerClockTrigger();
                await clockTrigger.AdvanceClock(GetClockName(), to);
            }
        }

        /// <summary>
        /// Allows awaiting delivery of all commands that are currently due on the command scheduler.
        /// </summary>
        public async Task CommandSchedulerDone()
        {
            if (builder.Configuration.IsUsingInMemoryCommandScheduling())
            {
                var virtualClock = Clock.Current as VirtualClock;
                if (virtualClock != null)
                {
                    await virtualClock.Done().TimeoutAfter(DefaultTimeout());
                }
            }
            else
            {
                var clockTrigger = builder.Configuration.SchedulerClockTrigger();

#pragma warning disable 618
                SchedulerAdvancedResult result;
#pragma warning restore 618
                do
                {
                    result = await clockTrigger.AdvanceClock(GetClockName(), Clock.Now()).TimeoutAfter(DefaultTimeout());
                } while (result.SuccessfulCommands.Any());
            }
        }

        internal static TimeSpan DefaultTimeout() =>
            TimeSpan.FromMinutes(!Debugger.IsAttached ? 1 : 15);

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        public void Save<TAggregate>(TAggregate aggregate)
            where TAggregate : class, IEventSourced =>
                SaveAsync(aggregate).Wait();

        /// <summary>
        ///     Persists the state of the specified aggregate by adding new events to the event store.
        /// </summary>
        /// <param name="aggregate">The aggregate to persist.</param>
        public async Task SaveAsync<TAggregate>(TAggregate aggregate)
            where TAggregate : class, IEventSourced
        {
            aggregates.Add(aggregate);
            await builder.GetRepository<TAggregate>().Save(aggregate);
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate. If null, and there's only a single aggregate of the specified type, it returns that; otherwise, it throws.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetLatest<TAggregate>(Guid? aggregateId = null)
            where TAggregate : class, IEventSourced =>
                GetLatestAsync<TAggregate>(aggregateId).Result;

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate. If null, and there's only a single aggregate of the specified type, it returns that; otherwise, it throws.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public async Task<TAggregate> GetLatestAsync<TAggregate>(Guid? aggregateId = null)
            where TAggregate : class, IEventSourced
        {
            if (!aggregateId.HasValue)
            {
                aggregateId = Aggregates.OfType<TAggregate>().Single().Id;
            }

            return await builder.GetRepository<TAggregate>()
                                .GetLatest(aggregateId.Value);
        }

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public TAggregate GetVersion<TAggregate>(Guid aggregateId, long version)
            where TAggregate : class, IEventSourced =>
                GetVersionAsync<TAggregate>(aggregateId, version).Result;

        /// <summary>
        ///     Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="version">The version at which to retrieve the aggregate.</param>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <returns>The deserialized aggregate, or null if none exists with the specified id.</returns>
        public async Task<TAggregate> GetVersionAsync<TAggregate>(Guid aggregateId, long version)
            where TAggregate : class, IEventSourced =>
                await builder.GetRepository<TAggregate>()
                             .GetVersion(aggregateId, version);

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        public TAggregate GetAsOfDate<TAggregate>(Guid aggregateId, DateTimeOffset asOfDate)
            where TAggregate : class, IEventSourced =>
                GetAsOfDateAsync<TAggregate>(aggregateId, asOfDate).Result;

        /// <summary>
        /// Finds and deserializes an aggregate the specified id, if any. If none exists, returns null.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate.</param>
        /// <param name="asOfDate">The date at which the aggregate should be sourced.</param>
        /// <returns>
        /// The deserialized aggregate, or null if none exists with the specified id.
        /// </returns>
        public async Task<TAggregate> GetAsOfDateAsync<TAggregate>(Guid aggregateId, DateTimeOffset asOfDate)
            where TAggregate : class, IEventSourced =>
                await builder.GetRepository<TAggregate>()
                             .GetAsOfDate(aggregateId, asOfDate);

        /// <summary>
        /// Registers an object for disposal when the scenario is disposed.
        /// </summary>
        public void RegisterForDispose(IDisposable disposable) => disposables.Add(disposable);

        private string GetClockName() =>
            builder.Configuration
                   .Properties
                   .IfContains("CommandSchedulerClockName")
                   .And()
                   .IfTypeIs<string>()
                   .ElseDefault();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => disposables.Dispose();
    }
}