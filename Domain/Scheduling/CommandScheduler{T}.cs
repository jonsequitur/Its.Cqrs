// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A basic command scheduler implementation that can be used as the basis for composing command scheduling behaviors.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    public class CommandScheduler<TAggregate> :
        ICommandScheduler<TAggregate>
        where TAggregate : class, IEventSourced
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

        /// <summary>
        /// Schedules the specified command.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command.</param>
        /// <returns>
        /// A task that is complete when the command has been successfully scheduled.
        /// </returns>
        /// <exception cref="System.NotSupportedException">Non-immediate scheduling is not supported.</exception>
        public virtual async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.IsDue() &&
                await VerifyPrecondition(scheduledCommand))
            {
                // resolve the command scheduler so that delivery goes through the whole pipeline
                await Configuration.Current.CommandScheduler<TAggregate>().Deliver(scheduledCommand);
                return;
            }

            if (scheduledCommand.Result == null)
            {
                throw new NotSupportedException("Deferred scheduling is not supported.");
            }
        }

        /// <summary>
        /// Delivers the specified scheduled command to the target aggregate.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command to be applied to the aggregate.</param>
        /// <returns>
        /// A task that is complete when the command has been applied.
        /// </returns>
        /// <remarks>
        /// The scheduler will apply the command and save it, potentially triggering additional consequences.
        /// </remarks>
        public virtual async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            await repository.ApplyScheduledCommand(scheduledCommand, preconditionVerifier);
        }

        /// <summary>
        /// Verifies that the command precondition has been met.
        /// </summary>
        protected async Task<bool> VerifyPrecondition(IScheduledCommand scheduledCommand)
        {
            return await preconditionVerifier.IsPreconditionSatisfied(scheduledCommand);
        }
    }
}