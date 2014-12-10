using System.Collections.Generic;

namespace Microsoft.Its.EventStore
{
    public class EventComparer : IEqualityComparer<IStoredEvent>
    {
        public bool Equals(IStoredEvent x, IStoredEvent y)
        {
            return x.AggregateId == y.AggregateId &&
                   x.SequenceNumber == y.SequenceNumber;
        }

        public int GetHashCode(IStoredEvent obj)
        {
            return (obj.AggregateId + "|" + obj).GetHashCode();
        }
    }
}