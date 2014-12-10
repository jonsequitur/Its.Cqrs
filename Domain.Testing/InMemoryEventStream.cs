using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.EventStore;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.EventStore;

namespace Microsoft.Its.Domain.Testing
{
    public class InMemoryEventStream : IEventStream
    {
        public HashSet<IStoredEvent> Events = new HashSet<IStoredEvent>(new Its.EventStore.EventComparer());

        public EventHandler<IStoredEvent> BeforeSave;

        public InMemoryEventStream(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }
            Name = name;
        }

        /// <summary>
        /// Gets the name of the event stream, i.e. the name of the aggregate type.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Appends an event to the stream.
        /// </summary>
        /// <param name="event">The event to append to the stream.</param>
        public void Append(IStoredEvent @event)
        {
            var handler = BeforeSave;
            if (handler != null)
            {
                handler(this, @event);
            }

            var storedEvent = @event.ToStoredEvent();

            if (Events.Contains(storedEvent))
            {
                throw new ConcurrencyException(string.Format("There was a concurrency violation.\n  Existing:\n{0}\nAttempted:\n{1}",
                                                             Events.Single(e => e.AggregateId == @event.AggregateId &&
                                                                                e.SequenceNumber == @event.SequenceNumber).ToDomainEvent(Name).ToDiagnosticJson(),
                                                             @event.ToDiagnosticJson()));
            }

            Events.Add(storedEvent);
        }

        /// <summary>
        /// Gets the latest event in the specified event stream.
        /// </summary>
        /// <returns></returns>
        public IStoredEvent Latest(string aggregateId)
        {
            return Events.Last(e => e.AggregateId == aggregateId);
        }

        /// <summary>
        /// Gets all of the events in the stream with the specified aggregate id.
        /// </summary>
        public IEnumerable<IStoredEvent> All(string aggregateId)
        {
            return Events.Where(e => e.AggregateId == aggregateId);
        }

        /// <summary>
        /// Gets all of the events in the stream created with the specified aggregate id as of the specified date.
        /// </summary>
        public IEnumerable<IStoredEvent> AsOfDate(string aggregateId, DateTimeOffset date)
        {
            return Events.Where(e => e.AggregateId == aggregateId)
                         .Where(e => e.Timestamp <= date);
        }

        /// <summary>
        /// Gets all of the events in the stream created with the specified aggregate id up to (and including) the specified version.
        /// </summary>
        public IEnumerable<IStoredEvent> UpToVersion(string aggregateId, long version)
        {
            return Events.Where(e => e.AggregateId == aggregateId)
                         .Where(e => e.SequenceNumber <= version);
        }
    }
}