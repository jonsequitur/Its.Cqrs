namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    public class CommandExecutionError
    {
        public long Id { get; set; }

        public string Error { get; set; }

        public ScheduledCommand ScheduledCommand { get; set; }
    }
}