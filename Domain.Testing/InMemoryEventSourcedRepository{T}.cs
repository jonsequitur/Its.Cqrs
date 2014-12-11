using System;
using System.Reactive.Linq;
using System.Linq;
using Microsoft.Its.Domain.EventStore;
using Microsoft.Its.EventStore;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    public class InMemoryEventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate> where TAggregate : class, IEventSourced
    {
        // TODO: (InMemoryEventSourcedRepository) this can probably be moved / copied to Domain.EventStore and used with any IEventStream implementation
        private readonly IEventStream eventStream;
        private readonly IEventBus bus;

        public InMemoryEventSourcedRepository(IEventStream eventStream = null, IEventBus bus = null)
        {
            this.eventStream = eventStream ?? new InMemoryEventStream(AggregateType<TAggregate>.EventStreamName);
            this.bus = bus ?? new FakeEventBus();
        }

        public TAggregate GetLatest(Guid aggregateId)
        {
            var events = eventStream.All(aggregateId.ToString())
                                    .Result
                                    .ToArray();

            if (events.Any())
            {
                return AggregateType<TAggregate>.Factory.Invoke(
                    aggregateId,
                    events.Select(e => e.ToDomainEvent(AggregateType<TAggregate>.EventStreamName)));
            }

            return null;
        }

        public TAggregate GetVersion(Guid aggregateId, long version)
        {
            var events = eventStream.UpToVersion(aggregateId.ToString(), version)
                                    .Result
                                    .ToArray();

            if (events.Any())
            {
                return events.CreateAggregate<TAggregate>();
            }

            return null;
        }

        public TAggregate GetAsOfDate(Guid aggregateId, DateTimeOffset asOfDate)
        {
            var events = eventStream.AsOfDate(aggregateId.ToString(), asOfDate)
                                    .Result
                                    .ToArray();

            if (events.Any())
            {
                return events.CreateAggregate<TAggregate>();
            }

            return null;
        }

        public void Save(TAggregate aggregate)
        {
            var events = aggregate.PendingEvents.ToArray();

            foreach (var e in events)
            {
                eventStream.Append(new[]
                {
                    e.ToStoredEvent()
                }).Wait();

                e.SetAggregate(aggregate);
            }

            // move pending events to the event history
            aggregate.IfTypeIs<EventSourcedAggregate>()
                     .ThenDo(a => a.ConfirmSave());

            // publish the events
            bus.PublishAsync(events)
               .Do(onNext: e => { },
                   onError: ex => Console.WriteLine(ex.ToString()))
               .Wait();
        }

        /// <summary>
        /// Refreshes an aggregate with the latest events from the event stream.
        /// </summary>
        /// <param name="aggregate">The aggregate to refresh.</param>
        /// <remarks>Events not present in the in-memory aggregate will not be re-fetched from the event store.</remarks>
        public void Refresh(TAggregate aggregate)
        {
            var newEvents = eventStream.All(id: aggregate.Id.ToString())
                                       .Result
                                       .Where(e => e.SequenceNumber > aggregate.Version)
                                       .Select(e => e.ToDomainEvent(AggregateType<TAggregate>.EventStreamName));

            aggregate.Update(newEvents);
        }
    }
}