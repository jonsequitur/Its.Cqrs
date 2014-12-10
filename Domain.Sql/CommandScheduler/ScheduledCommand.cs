using System;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ScheduledCommand
    {
        public Guid AggregateId { get; set; }

        public long SequenceNumber { get; set; }

        public string AggregateType { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public DateTimeOffset? DueTime { get; set; }

        public DateTimeOffset? AppliedTime { get; set; }

        public DateTimeOffset? FinalAttemptTime { get; set; }

        public string SerializedCommand { get; set; }

        public int Attempts { get; set; }

        public Clock Clock { get; set; }

        public ScheduledCommandResult Result { get; set; }
    }
}