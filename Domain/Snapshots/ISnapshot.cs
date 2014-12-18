using System;

namespace Microsoft.Its.Domain
{
    public interface ISnapshot
    {
        Guid AggregateId { get; set; }
        long Version { get; set; }
        DateTimeOffset LastUpdated { get; set; }
        string AggregateTypeName { get; set; }
        string[] ETags { get; set; }
    }
}