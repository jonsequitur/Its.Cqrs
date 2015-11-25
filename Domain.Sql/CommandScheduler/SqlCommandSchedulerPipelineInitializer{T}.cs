// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SqlCommandSchedulerPipelineInitializer :
        SchedulerPipelineInitializer
    {
        private readonly Func<CommandSchedulerDbContext> createDbContext;
        private readonly Func<GetClockName> getClockName;

        public SqlCommandSchedulerPipelineInitializer(
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

        protected override void InitializeFor<TAggregate>(Configuration configuration)
        {
            configuration
                .AddToCommandSchedulerPipeline<TAggregate>(
                    schedule: async (cmd, next) => await Schedule(cmd, next),
                    deliver: async (cmd, next) => await Deliver(cmd, next));
        }

        private async Task Schedule<TAggregate>(IScheduledCommand<TAggregate> cmd, Func<IScheduledCommand<TAggregate>, Task> next)
            where TAggregate : class, IEventSourced
        {
            var storedCommand = await Storage.StoreScheduledCommand(
                cmd,
                createDbContext,
                GetClockName);

            if (cmd.IsDue(storedCommand.Clock))
            {
                var preconditionVerifier = Configuration.Current.CommandPreconditionVerifier();

                // sometimes the command depends on a precondition event that hasn't been saved
                if (await preconditionVerifier.IsPreconditionSatisfied(cmd))
                {
                    await Configuration.Current.CommandScheduler<TAggregate>().Deliver(cmd);
                }
            }

            await next(cmd);
        }

        private async Task Deliver<TAggregate>(IScheduledCommand<TAggregate> cmd, Func<IScheduledCommand<TAggregate>, Task> next)
            where TAggregate : class, IEventSourced
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
            IScheduledCommand scheduledCommand,
            CommandSchedulerDbContext dbContext)
        {
            return getClockName()(scheduledCommand);
        }
    }
}