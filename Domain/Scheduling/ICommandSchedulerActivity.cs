namespace Microsoft.Its.Domain
{
    public interface ICommandSchedulerActivity
    {
        IScheduledCommand ScheduledCommand { get; }
    }
}
