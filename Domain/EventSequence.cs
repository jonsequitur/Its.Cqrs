// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// Coordinates sequence numbers for a sequence of events associated with the same aggregate id. 
    /// </summary>
    [DebuggerStepThrough]
    public class EventSequence : IEnumerable<IEvent>
    {
        private readonly HashSet<IEvent> events = new HashSet<IEvent>(EventComparer.Instance);
        private readonly Guid aggregateId;
        private long version;
        private long startSequenceFrom;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSequence"/> class.
        /// </summary>
        /// <param name="aggregateId">The id of the aggregate that the events belong to.</param>
        public EventSequence(Guid aggregateId)
        {
            startSequenceFrom = 0;
            this.aggregateId = aggregateId;
        }

        /// <summary>
        /// Adds the specified event to the sequence.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <exception cref="System.ArgumentNullException">event</exception>
        /// <exception cref="System.ArgumentException">Event must have a non-empty AggregateId property or inherit from Event so that it can be set automatically.
        /// or
        /// Inconsistent aggregate ids. Previous events have aggregate id x but the one being added has aggregate id y.</exception>
        public void Add(IEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException("event");
            }

            var e = @event as Event;
            if (@event.AggregateId == Guid.Empty)
            {
                if (e != null)
                {
                    e.AggregateId = aggregateId;
                }
                else
                {
                    throw new ArgumentException("Event must have a non-empty AggregateId property or inherit from Event so that it can be set automatically.");
                }
            }
            else if (@event.AggregateId != aggregateId)
            {
                throw new ArgumentException(
                    string.Format("Inconsistent aggregate ids. Previous events have aggregate id {0} but the one being added has aggregate id {1}.",
                                  aggregateId,
                                  @event.AggregateId));
            }

            if (@event.SequenceNumber <= 0)
            {
                if (e != null)
                {
                    e.SequenceNumber = Math.Max(version + 1, events.Count + 1 + startSequenceFrom);
                }
                else
                {
                    throw new ArgumentException("Event must have a positive SequenceNumber property or inherit from Event so that it can be set automatically.");
                }
            }
            else if (events.Contains(@event))
            {
                throw new ArgumentException(string.Format("Event with SequenceNumber {0} is already present in the sequence.",
                                                          @event.SequenceNumber));
            }

            version = Math.Max(version, @event.SequenceNumber);

            events.Add(@event);
        }

        /// <summary>
        /// Gets the count of events in the sequence.
        /// </summary>
        public long Count
        {
            get
            {
                return events.Count;
            }
        }

        /// <summary>
        /// Gets the id of the aggregate to which these events belong.
        /// </summary>
        public Guid AggregateId
        {
            get
            {
                return aggregateId;
            }
        }

        /// <summary>
        /// Gets the version the sequence is at, which is the SequenceNumber of the last event in the sequence.
        /// </summary>
        public long Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IEvent> GetEnumerator()
        {
            return events.OrderBy(e => e.SequenceNumber).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a number of events to the sequence.
        /// </summary>
        /// <param name="eventHistory">The events to add.</param>
        /// <exception cref="System.ArgumentException"><paramref name="eventHistory" /> is empty.</exception>
        public void AddRange(IEnumerable<IEvent> eventHistory)
        {
            foreach (var @event in eventHistory)
            {
                Add(@event);
            }
        }

        internal void SetVersion(long version)
        {
            if (Count > 0)
            {
                throw new InvalidOperationException("The version must be set before events are added.");
            }

            if (version < 0)
            {
                throw new ArgumentException("version must be at least 0.");
            }

            startSequenceFrom = version;
            this.version = version;
        }

        internal void TransferTo(EventSequence eventHistory)
        {
            eventHistory.AddRange(this);
            events.Clear();
            startSequenceFrom = eventHistory.Version;
            version = eventHistory.Version;
        }
    }
}
