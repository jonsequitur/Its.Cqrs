// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public class CommandScheduler<TAggregate> : ICommandScheduler<TAggregate> where TAggregate : class, IEventSourced
    {
        protected readonly IEventSourcedRepository<TAggregate> repository;
        private readonly ICommandPreconditionVerifier preconditionVerifier;

        public CommandScheduler(
            IEventSourcedRepository<TAggregate> repository,
            ICommandPreconditionVerifier preconditionVerifier = null)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            this.repository = repository;
            this.preconditionVerifier = preconditionVerifier ??
                                        Configuration.Current.Container.Resolve<ICommandPreconditionVerifier>();
        }

        public virtual async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.IsDue() &&
                await VerifyPrecondition(scheduledCommand))
            {
                await Configuration.Current.CommandScheduler<TAggregate>().Deliver(scheduledCommand);
                return;
            }

            if (scheduledCommand.Result == null)
            {
                throw new NotSupportedException("Non-immediate scheduling is not supported.");
            }
        }

        public virtual async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await repository.ApplyScheduledCommand(scheduledCommand, preconditionVerifier);
        }

        protected async Task<bool> VerifyPrecondition(IScheduledCommand scheduledCommand)
        {
            return await preconditionVerifier.IsPreconditionSatisfied(scheduledCommand);
        }
    }
}