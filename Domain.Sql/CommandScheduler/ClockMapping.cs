namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class ClockMapping
    {
        public long Id { get; set; }

        public Clock Clock { get; set; }

        public string Value { get; set; }
    }
}