using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Recipes;
using Its.Validation;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Provides additional functionality for event-sourced aggregates.
    /// </summary>
    [DebuggerStepThrough]
    public static class AggregateExtensions
    {
        /// <summary>
        ///     Applies a command to an aggregate.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="command">The command.</param>
        /// <returns>The same aggregate with the command applied and any applicable updates performed.</returns>
        public static TAggregate Apply<TAggregate>(
            this TAggregate aggregate,
            ICommand<TAggregate> command)
            where TAggregate : class
        {
            command.ApplyTo(aggregate);
            return aggregate;
        }

        /// <summary>
        /// Gets an event sequence containing both the event history and pending events for the specified aggregate.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <returns></returns>
        public static IEnumerable<IEvent> Events(this EventSourcedAggregate aggregate)
        {
            return aggregate.EventHistory.Concat(aggregate.PendingEvents);
        }

        /// <summary>
        /// Determines whether an aggregate is valid for application of the specified command.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="command">The command.</param>
        /// <returns>
        ///   <c>true</c> if the command can be applied; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsValidTo<TAggregate>(this TAggregate aggregate, Command<TAggregate> command)
            where TAggregate : class
        {
            return !command.RunAllValidations(aggregate, false).HasFailures;
        }

        public static TAggregate AsOfVersion<TAggregate>(this TAggregate aggregate, long version) where TAggregate : EventSourcedAggregate
        {
            return AggregateType<TAggregate>.Factory.Invoke(
                aggregate.Id,
                aggregate.Events()
                         .Where(e => e.SequenceNumber <= version));
        }

        /// <summary>
        /// Updates the specified aggregate with additional events.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">events</exception>
        /// <exception cref="System.InvalidOperationException">Aggregates having pending events cannot be updated.</exception>
        /// <remarks>This method can be used when additional events have been appended to an event stream and you would like to bring an in-memory aggregate up to date with those events. If there are new pending events, the aggregate needs to be reset first, and any commands re-applied.</remarks>
        internal static void Update<TAggregate>(
            this TAggregate aggregate,
            IEnumerable<IEvent> events)
            where TAggregate : IEventSourced
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            if (aggregate.PendingEvents.Any())
            {
                throw new InvalidOperationException("Aggregates having pending events cannot be updated.");
            }

            var startingVersion = aggregate.Version;

            var pendingEvents = aggregate.PendingEvents
                                         .IfTypeIs<EventSequence>()
                                         .ElseDefault();

            foreach (var @event in events
                .OfType<IEvent<TAggregate>>()
                .Where(e => e.SequenceNumber > startingVersion)
                .Do(e =>
                {
                    if (e.SequenceNumber == 0)
                    {
                        throw new InvalidOperationException("Event has not been previously stored: " + e.ToJson());
                    }
                })
                .ToArray())
            {
                pendingEvents.Add(@event);
                @event.Update(aggregate);
            }

            aggregate.IfTypeIs<EventSourcedAggregate>()
                     .ThenDo(a => a.ConfirmSave());
        }

        /// <summary>
        /// Validates the command against the specified aggregate.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="command">The command.</param>
        /// <returns>A <see cref="ValidationReport" /> detailing any validation errors.</returns>
        public static ValidationReport Validate<TAggregate>(this TAggregate aggregate, Command<TAggregate> command)
            where TAggregate : class
        {
            return command.RunAllValidations(aggregate, false);
        }

        /// <summary>
        /// Returns the version number of the aggregate, which is equal to it's latest event's sequence id.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <returns>The aggregate's version.</returns>
        public static long Version<TAggregate>(this TAggregate aggregate)
            where TAggregate : EventSourcedAggregate
        {
            if (aggregate == null)
            {
                throw new ArgumentNullException("aggregate");
            }

            return Math.Max(
                ((EventSequence) aggregate.EventHistory).Version,
                ((EventSequence) aggregate.PendingEvents).Version);
        }

        public static bool HasETag<TAggregate>(this TAggregate aggregate, string etag)
            where TAggregate : IEventSourced
        {
            if (string.IsNullOrWhiteSpace(etag))
            {
                return false;
            }

            var eventSourced = aggregate as EventSourcedAggregate;

            return eventSourced != null &&
                   eventSourced.Events().Any(e => e.ETag == etag);
        }
    }
}