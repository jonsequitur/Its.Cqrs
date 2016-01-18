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
    public class InMemoryEventStream : IEnumerable<IStoredEvent>
    {
        private readonly HashSet<IStoredEvent> events = new HashSet<IStoredEvent>(new EventComparer());

        public EventHandler<IStoredEvent> BeforeSave;

        public IEnumerable<IStoredEvent> Events
        {
            get
            {
                return events;
            }
        }

        public async Task Append(IStoredEvent[] @events)
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

        public void RemoveEvents(Guid aggregateId)
        {
            events.RemoveWhere(e => e.AggregateId == aggregateId.ToString());
        }

        private void ThrowConcurrencyException(IStoredEvent storedEvent)
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
                string.Format(
                    @"There was a concurrency violation.
Existing:
{0}
Attempted:
{1}",
                    existing,
                    attempted));
        }

        public async Task<IEnumerable<IStoredEvent>> All(string id)
        {
            return await Task.Run(() => events.Where(e => e.AggregateId == id));
        }

        public async Task<IEnumerable<IStoredEvent>> AsOfDate(string id, DateTimeOffset date)
        {
            return await Task.Run(() => events.Where(e => e.AggregateId == id)
                .Where(e => e.Timestamp <= date));
        }

        public async Task<IEnumerable<IStoredEvent>> UpToVersion(string id, long version)
        {
            return await Task.Run(() => events.Where(e => e.AggregateId == id)
                .Where(e => e.SequenceNumber <= version));
        }

        public IEnumerator<IStoredEvent> GetEnumerator()
        {
            return events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
