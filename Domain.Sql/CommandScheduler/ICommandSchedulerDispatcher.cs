using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal interface ICommandSchedulerDispatcher : IEventHandlerBinder
    {
        string AggregateType { get; }

        Task Deliver(ScheduledCommand scheduled);
    }
}