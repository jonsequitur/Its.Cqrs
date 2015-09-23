using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Testing
{
    internal class InMemoryCommandSchedulerPipelineInitializer<TAggregate> : ISchedulerPipelineInitializer
        where TAggregate : class, IEventSourced
    {
        public void Initialize(Configuration configuration)
        {
            configuration.IsUsingCommandSchedulerPipeline(true);

            configuration.AddToCommandSchedulerPipeline(
                             CommandScheduler.WithInMemoryDeferredScheduling<TAggregate>(configuration))
                         .TraceCommandsFor<TAggregate>();
        }
    }
}