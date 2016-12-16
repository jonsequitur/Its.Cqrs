// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// An in-memory stream of events.
    /// </summary>
    public class InMemoryEventStream : IEnumerable<InMemoryStoredEvent>
    {
        private readonly HashSet<InMemoryStoredEvent> events = new HashSet<InMemoryStoredEvent>();

        internal long NextAbsoluteSequenceNumber = 0;

        /// <summary>
        /// An event triggered before events are saved to the stream.
        /// </summary>
        public EventHandler<InMemoryStoredEvent> BeforeSave;

        /// <summary>
        /// Gets the sequence of events within the stream.
        /// </summary>
        public IEnumerable<InMemoryStoredEvent> Events => events;

        /// <summary>
        /// Appends the specified events to the stream.
        /// </summary>
        /// <param name="events">The events to append.</param>
        public async Task Append(InMemoryStoredEvent[] @events)
        {
            await Task.Run(() =>
            {
                var handler = BeforeSave;
                if (handler != null)
                {
                    @events.ForEach(e => handler(this, e));
                }

                lock (this.events)
                {
                    foreach (var storedEvent in events)
                    {
                        if (this.events.Contains(storedEvent))
                        {
                            ThrowConcurrencyException(storedEvent);
                        }
                        this.events.Add(storedEvent);
                    }
                }
            });
        }

        /// <summary>
        /// Removes events from the stream by aggregate id.
        /// </summary>
        /// <param name="aggregateId">The aggregate id of the events to be removed.</param>
        public void RemoveEvents(Guid aggregateId) =>
            events.RemoveWhere(e => e.AggregateId == aggregateId.ToString());

        private void ThrowConcurrencyException(InMemoryStoredEvent storedEvent)
        {
            var existing = events.Single(
                e => e.AggregateId == storedEvent.AggregateId &&
                     e.SequenceNumber == storedEvent.SequenceNumber)
                                 .ToDomainEvent()
                                 .ToDiagnosticJson();

            var attempted = storedEvent
                .ToDomainEvent()
                .ToDiagnosticJson();

            throw new ConcurrencyException(
                $@"There was a concurrency violation.
Existing:
{existing}
Attempted:
{attempted}");
        }

        /// <summary>
        /// Gets all events having the specified aggregate id.
        /// </summary>
        public async Task<IEnumerable<InMemoryStoredEvent>> All(string id) =>
            await Task.Run(() => events.Where(e => e.AggregateId == id));

        /// <summary>
        /// Gets all events recoded as of a given date having the specified aggregate id.
        /// </summary>
        public async Task<IEnumerable<InMemoryStoredEvent>> AsOfDate(string id, DateTimeOffset date) =>
            await Task.Run(() => events.Where(e => e.AggregateId == id)
                                       .Where(e => e.Timestamp <= date));

        /// <summary>
        /// Gets all events recoded as of a given version having the specified aggregate id.
        /// </summary>
        public async Task<IEnumerable<InMemoryStoredEvent>> UpToVersion(string id, long version) =>
            await Task.Run(() => events.Where(e => e.AggregateId == id)
                                       .Where(e => e.SequenceNumber <= version));

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public virtual IEnumerator<InMemoryStoredEvent> GetEnumerator() => events.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}