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
    public class InMemoryEventStream : IEnumerable<InMemoryStoredEvent>
    {
        private readonly HashSet<InMemoryStoredEvent> events = new HashSet<InMemoryStoredEvent>();

        internal long NextAbsoluteSequenceNumber = 0;

        public EventHandler<InMemoryStoredEvent> BeforeSave;

        public IEnumerable<InMemoryStoredEvent> Events => events;

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

        public async Task<IEnumerable<InMemoryStoredEvent>> All(string id) =>
            await Task.Run(() => events.Where(e => e.AggregateId == id));

        public async Task<IEnumerable<InMemoryStoredEvent>> AsOfDate(string id, DateTimeOffset date) =>
            await Task.Run(() => events.Where(e => e.AggregateId == id)
                                       .Where(e => e.Timestamp <= date));

        public async Task<IEnumerable<InMemoryStoredEvent>> UpToVersion(string id, long version) =>
            await Task.Run(() => events.Where(e => e.AggregateId == id)
                                       .Where(e => e.SequenceNumber <= version));

        public IEnumerator<InMemoryStoredEvent> GetEnumerator() => events.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}