// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        ///     Applies a command to an aggregate.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="command">The command.</param>
        /// <returns>The same aggregate with the command applied and any applicable updates performed.</returns>
        public static async Task<TAggregate> ApplyAsync<TAggregate>(
            this TAggregate aggregate,
            ICommand<TAggregate> command)
            where TAggregate : class
        {
            await command.ApplyToAsync(aggregate);
            return aggregate;
        }

        /// <summary>
        /// Gets an event sequence containing both the event history and pending events for the specified aggregate.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <returns></returns>
        public static IEnumerable<IEvent> Events(this EventSourcedAggregate aggregate)
        {
            if (aggregate.sourceSnapshot != null)
            {
                throw new InvalidOperationException("Aggregate was sourced from a snapshot, so event history is unavailable.");
            }

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

        /// <summary>
        /// Creates a new instance of the aggregate in memory using its state as of the specified version.
        /// </summary>
        public static TAggregate AsOfVersion<TAggregate>(this TAggregate aggregate, long version) where TAggregate : EventSourcedAggregate
        {
            var snapshot = aggregate.sourceSnapshot;

            var eventsAsOfVersion = aggregate.EventHistory
                                             .Concat(aggregate.PendingEvents)
                                             .Where(e => e.SequenceNumber <= version);

            if (snapshot != null)
            {
                if (snapshot.Version > version)
                {
                    throw new InvalidOperationException("Snapshot version is later than specified version. Source the aggregate from an earlier snapshot or from events in order to use AsOfVersion.");
                }

                return AggregateType<TAggregate>.FromSnapshot.Invoke(
                    snapshot,
                    eventsAsOfVersion);
            }

            return AggregateType<TAggregate>.FromEventHistory.Invoke(
                aggregate.Id,
                eventsAsOfVersion);
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

        /// <summary>
        /// Determines whether the specified ETag already exists in the aggregate's event stream.
        /// </summary>
        /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="etag">The etag.</param>
        public static bool HasETag<TAggregate>(this TAggregate aggregate, string etag)
            where TAggregate : IEventSourced
        {
            if (string.IsNullOrWhiteSpace(etag))
            {
                return false;
            }

            return aggregate.IfTypeIs<EventSourcedAggregate>()
                            .Then(a => a.HasETag(etag))
                            .ElseDefault();
        }

        internal static IEnumerable<string> ETags(this IEventSourced eventSourced)
        {
            return eventSourced.IfTypeIs<EventSourcedAggregate>()
                               .Then(a => a.ETags())
                               .Else(() => new string[0]);
        }
    }
}
