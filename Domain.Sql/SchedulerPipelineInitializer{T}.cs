using System;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Sql
{
    internal class SchedulerPipelineInitializer<TAggregate> :
        ISchedulerPipelineInitializer
        where TAggregate : class, IEventSourced
    {
        private readonly Configuration configuration;
        private readonly Func<CommandSchedulerDbContext> createDbContext;
        private readonly Func<GetClockName> getClockName;

        public SchedulerPipelineInitializer(
            Configuration configuration,
            Func<CommandSchedulerDbContext> createDbContext,
            Func<GetClockName> getClockName)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }
            if (createDbContext == null)
            {
                throw new ArgumentNullException("createDbContext");
            }
            this.configuration = configuration;
            this.createDbContext = createDbContext;
            this.getClockName = getClockName;
        }

        public void Initialize()
        {
            configuration.UseCommandSchedulerPipeline<TAggregate>(
                scheduler => scheduler.Wrap(
                    schedule: async (cmd, next) =>
                    {
                        await Storage.StoreScheduledCommand(
                            cmd,
                            createDbContext,
                            GetClockName);

                        await next(cmd);
                    },
                    deliver: async (cmd, next) => { await next(cmd); }));

            var consequenter = Consequenter.Create<IScheduledCommand<TAggregate>>(e => { Task.Run(() => configuration.CommandScheduler<TAggregate>().Schedule(e)).Wait(); });

            var subscription = configuration.EventBus.Subscribe(consequenter);

            configuration.RegisterForDisposal(subscription);
        }

        private async Task<string> GetClockName(
            IScheduledCommand<TAggregate> scheduledCommand,
            CommandSchedulerDbContext dbContext)
        {
            return getClockName()(scheduledCommand);
        }
    }
}