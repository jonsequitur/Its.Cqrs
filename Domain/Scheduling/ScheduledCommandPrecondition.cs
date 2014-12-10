using System;

namespace Microsoft.Its.Domain
{
    public class ScheduledCommandPrecondition
    {
        public Guid AggregateId { get; set; }
        public string ETag { get; set; }
    }
}