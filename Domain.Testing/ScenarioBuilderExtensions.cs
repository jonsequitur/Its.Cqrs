using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// Methods for setting up a <see cref="ScenarioBuilder" />.
    /// </summary>
    public static class ScenarioBuilderExtensions
    {
        /// <summary>
        /// Adds events to the scenario, which will be saved to the event stream and send to any subscribed handlers when <see cref="ScenarioBuilder.Prepare" /> is called.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="builder">The scenario builder.</param>
        /// <param name="events">The events.</param>
        /// <returns>The same scenario builder.</returns>
        public static TScenarioBuilder AddEvents<TScenarioBuilder>(
            this TScenarioBuilder builder,
            params IEvent[] events)
            where TScenarioBuilder : ScenarioBuilder
        {
            builder.EnsureScenarioHasNotBeenPrepared();

            foreach (var e in events)
            {
                // set the aggregate id
                if (e.AggregateId == Guid.Empty)
                {
                    e.IfTypeIs<Event>()
                     .ThenDo(ev =>
                             ev.AggregateId = builder.defaultAggregateIdsByType.GetOrAdd(e.AggregateType(),
                                                                                         t => Guid.NewGuid()))
                     .ElseDo(() => { throw new ArgumentException("When using IEvent implementations not derived from Event, you must specify a non-empty AggregateId."); });
                }

                builder.events.Add(e);
            }

            return builder;
        }

        /// <summary>
        /// Adds a handler (such as a projector or consequenter) to the scenario and subscribes it to the scenario's event bus.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="builder">The scenario builder.</param>
        /// <param name="handler">The handler.</param>
        /// <returns>The same scenario builder.</returns>
        public static TScenarioBuilder AddHandler<TScenarioBuilder>(
            this TScenarioBuilder builder, object handler)
            where TScenarioBuilder : ScenarioBuilder
        {
            builder.handlers.Add(handler);

            if (builder.prepared)
            {
                builder.EventBus.Subscribe(handler);
            }

            return builder;
        }

        /// <summary>
        /// Adds a handler (such as a projector or consequenter) to the scenario and subscribes it to the scenario's event bus.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="builder">The scenario builder.</param>
        /// <param name="handlerTypes">The types of the handlers to be added.</param>
        /// <returns>
        /// The same scenario builder.
        /// </returns>
        public static TScenarioBuilder AddHandlers<TScenarioBuilder>(
            this TScenarioBuilder builder,
            params Type[] handlerTypes)
            where TScenarioBuilder : ScenarioBuilder
        {
            Action instantiateAndSubscribe = () =>
            {
                foreach (var type in handlerTypes.OrEmpty())
                {
                    builder.AddHandler(builder.Configuration.Container.Resolve(type));
                }
            };

            if (builder.prepared)
            {
                instantiateAndSubscribe();
            }
            else
            {
                builder.BeforePrepare(instantiateAndSubscribe);
            }

            return builder;
        }

        /// <summary>
        /// Advances the clock used for setting event timestamps in the scenario.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="scenarioBuilder">The scenario builder.</param>
        /// <param name="timeSpan">The amount of time by which to advance the scenario clock.</param>
        public static TScenarioBuilder AdvanceClockBy<TScenarioBuilder>(
            this TScenarioBuilder scenarioBuilder,
            TimeSpan timeSpan)
            where TScenarioBuilder : ScenarioBuilder
        {
            VirtualClock.Current.AdvanceBy(timeSpan);
            return scenarioBuilder;
        }

        /// <summary>
        /// Sets the clock used for setting event timestamps in the scenario.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="scenarioBuilder">The scenario builder.</param>
        /// <param name="time">The time to set the scenario clock to.</param>
        public static TScenarioBuilder AdvanceClockTo<TScenarioBuilder>(
            this TScenarioBuilder scenarioBuilder,
            DateTimeOffset time)
            where TScenarioBuilder : ScenarioBuilder
        {
            VirtualClock.Current.AdvanceTo(time);
            return scenarioBuilder;
        }

        /// <summary>
        /// Gets the time at which the scenario's timeline begins.
        /// </summary>
        /// <remarks>This is the earliest of either the virtual clock time or the earliest event in the <see cref="ScenarioBuilder.InitialEvents" /> sequence.</remarks>
        public static DateTimeOffset StartTime(this ScenarioBuilder scenarioBuilder)
        {
            var earliestEvent = scenarioBuilder.InitialEvents
                                               .OrderBy(e => e.Timestamp)
                                               .FirstOrDefault();

            var now = VirtualClock.Current.Now();

            if (earliestEvent == null)
            {
                return now;
            }

            return now.UtcTicks < earliestEvent.Timestamp.UtcTicks
                       ? now
                       : earliestEvent.Timestamp;
        }

        /// <summary>
        /// Indicates that the scenario should persist events to a SQL-based event store via <see cref="EventStoreDbContext" />.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="builder">The scenario builder.</param>
        /// <returns>The same scenario builder.</returns>
        public static TScenarioBuilder UseSqlEventStore<TScenarioBuilder>(
            this TScenarioBuilder builder)
            where TScenarioBuilder : ScenarioBuilder
        {
            builder.EnsureScenarioHasNotBeenPrepared();
            builder.UseSqlEventStore();
            return builder;
        }

        /// <summary>
        /// Indicates that the scenario should schedule commands using a SQL-based command scheduler.
        /// </summary>
        /// <typeparam name="TScenarioBuilder">The type of the scenario builder.</typeparam>
        /// <param name="builder">The scenario builder.</param>
        /// <returns>
        /// The same scenario builder
        /// </returns>
        public static TScenarioBuilder UseSqlCommandScheduler<TScenarioBuilder>(
            this TScenarioBuilder builder)
            where TScenarioBuilder : ScenarioBuilder
        {
            builder.EnsureScenarioHasNotBeenPrepared();
            builder.useInMemoryCommandScheduling = false;

            builder.Configuration.UseSqlCommandScheduling();

            var scheduler = builder.Configuration.SqlCommandScheduler();

            var clockName = Guid.NewGuid().ToString();
            scheduler.CreateClock(clockName, DateTimeOffset.Parse("1970-01-01 12:00:00 +00:00"));
            builder.Configuration.Properties["CommandSchedulerClockName"] = clockName;
            scheduler.GetClockName = @event => clockName;

            return builder;
        }
    }
}