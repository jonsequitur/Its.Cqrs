using System;
using Microsoft.Its.EventStore;

namespace Microsoft.Its.Domain.EventStore
{
    /// <summary>
    /// Provides information about an error that occurs while handling an event.
    /// </summary>
    public class EventHandlingError
    {
        public EventHandlingError(IStoredEvent e, string streamName)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            AggregateId = Guid.Parse(e.AggregateId);
            StreamName = streamName;
            EventTypeName = e.Type;
            SequenceNumber = e.SequenceNumber;
            Body = e.Body;
            Timestamp = e.Timestamp;
        }

        protected DateTimeOffset Timestamp { get; set; }

        protected long SequenceNumber { get; set; }

        protected Guid AggregateId { get; set; }

        public string Body { get; set; }

        public string EventTypeName { get; set; }

        public string StreamName { get; set; }

        public string Error { get; set; }
    }
}