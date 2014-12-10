using System.Collections.Generic;

namespace Microsoft.Its.Domain
{
    internal interface IEventQuery
    {
        IEnumerable<MatchEvent> IncludedEventTypes { get; }
    }
}