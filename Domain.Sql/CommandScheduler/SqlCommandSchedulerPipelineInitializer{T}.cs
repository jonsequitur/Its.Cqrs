// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SqlCommandSchedulerPipelineInitializer :
        CommandSchedulerPipelineInitializer
    {
        private readonly Func<CommandSchedulerDbContext> createDbContext;
        private readonly Func<GetClockName> getClockName;

        public SqlCommandSchedulerPipelineInitializer(
            Func<CommandSchedulerDbContext> createDbContext,
            Func<GetClockName> getClockName)
        {
            if (createDbContext == null)
            {
                throw new ArgumentNullException(nameof(createDbContext));
            }
            this.createDbContext = createDbContext;
            this.getClockName = getClockName;
        }

        protected override void InitializeFor<TAggregate>(Configuration configuration) =>
            configuration
                .AddToCommandSchedulerPipeline<TAggregate>(
                    schedule: async (cmd, next) => await Schedule(cmd, next),
                    deliver: async (cmd, next) => await Deliver(cmd, next));

        private async Task Schedule<TAggregate>(
            IScheduledCommand<TAggregate> cmd,
            Func<IScheduledCommand<TAggregate>, Task> next)
            where TAggregate : class
        {
            await Storage.StoreScheduledCommand(
                cmd,
                createDbContext,
                GetClockName);

            await next(cmd);
        }

        private async Task Deliver<TAggregate>(
            IScheduledCommand<TAggregate> cmd,
            Func<IScheduledCommand<TAggregate>, Task> next)
            where TAggregate : class
        {
            IClock clock;
            if (cmd.DueTime != null)
            {
                clock = Domain.Clock.Create(() => cmd.DueTime.Value);
            }
            else
            {
                clock = cmd.Clock;
            }

            using (CommandContext.Establish(cmd.Command, clock))
            {
                await next(cmd);

                if (!cmd.Command.RequiresDurableScheduling())
                {
                    return;
                }

                await Storage.UpdateScheduledCommand(
                    cmd,
                    createDbContext);
            }
        }

        private Task<string> GetClockName(
            IScheduledCommand scheduledCommand,
            CommandSchedulerDbContext dbContext) =>
                Task.FromResult(getClockName()(scheduledCommand));
    }
}