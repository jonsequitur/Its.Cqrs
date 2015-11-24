namespace Microsoft.Its.Domain.Testing
{
    internal class InMemoryCommandSchedulerPipelineInitializer : SchedulerPipelineInitializer
    {
        protected override void InitializeFor<TAggregate>(Configuration configuration)
        {
            configuration.IsUsingCommandSchedulerPipeline(true)
                         .IsUsingInMemoryCommandScheduling(true);

            configuration.AddToCommandSchedulerPipeline(
                CommandScheduler.WithInMemoryDeferredScheduling<TAggregate>(configuration));
        }
    }
}