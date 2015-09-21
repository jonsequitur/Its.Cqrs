using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SchedulerPipelineInitializer<TAggregate> :
        ISchedulerPipelineInitializer
        where TAggregate : class, IEventSourced
    {
        private readonly Func<CommandSchedulerDbContext> createDbContext;
        private readonly Func<GetClockName> getClockName;

        public SchedulerPipelineInitializer(
            Func<CommandSchedulerDbContext> createDbContext,
            Func<GetClockName> getClockName)
        {
            if (createDbContext == null)
            {
                throw new ArgumentNullException("createDbContext");
            }
            this.createDbContext = createDbContext;
            this.getClockName = getClockName;
        }

        public void Initialize(Configuration configuration)
        {
            configuration.AddToCommandSchedulerPipeline<TAggregate>(
                schedule: async (cmd, next) => await Schedule(cmd, next),
                deliver: async (cmd, next) => await Deliver(cmd, next));

            configuration.SubscribeCommandSchedulerToEventBusFor<TAggregate>();
        }

        private async Task Schedule(IScheduledCommand<TAggregate> cmd, Func<IScheduledCommand<TAggregate>, Task> next)
        {
            var storedCommand = await Storage.StoreScheduledCommand(
                cmd,
                createDbContext,
                GetClockName);

            if (cmd.IsDue(storedCommand.Clock))
            {
                var preconditionVerifier = Configuration.Current.Container.Resolve<ICommandPreconditionVerifier>();

                // sometimes the command depends on a precondition event that hasn't been saved
                if (await preconditionVerifier.IsPreconditionSatisfied(cmd))
                {
                    await Configuration.Current.CommandScheduler<TAggregate>().Deliver(cmd);
                }
            }

            await next(cmd);
        }

        private async Task Deliver(IScheduledCommand<TAggregate> cmd, Func<IScheduledCommand<TAggregate>, Task> next)
        {
            IClock clock = null;
            if (cmd.DueTime != null)
            {
                clock = Domain.Clock.Create(() => cmd.DueTime.Value);
            }

            using (CommandContext.Establish(cmd.Command, clock))
            {
                await next(cmd);

                if (!cmd.Command.RequiresDurableScheduling)
                {
                    return;
                }

                await Storage.UpdateScheduledCommand(
                    cmd,
                    createDbContext);
            }
        }

        private async Task<string> GetClockName(
            IScheduledCommand<TAggregate> scheduledCommand,
            CommandSchedulerDbContext dbContext)
        {
            return getClockName()(scheduledCommand);
        }
    }
}