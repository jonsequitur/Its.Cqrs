using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    internal class EventComparer : IEqualityComparer<IEvent>
    {
        public static readonly EventComparer Instance = new EventComparer(); 

        public bool Equals(IEvent x, IEvent y)
        {
            return x.AggregateId == y.AggregateId &&
                   x.SequenceNumber == y.SequenceNumber;
        }

        public int GetHashCode(IEvent obj)
        {
            return (obj.AggregateId + "|" + obj).GetHashCode();
        }
    }
}