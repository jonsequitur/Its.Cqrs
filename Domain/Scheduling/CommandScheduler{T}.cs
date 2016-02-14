// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A basic command scheduler implementation that can be used as the basis for composing command scheduling behaviors.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the command target.</typeparam>
    internal class CommandScheduler<TAggregate> : ICommandScheduler<TAggregate> 
        where TAggregate : class
    {
        private readonly IStore<TAggregate> store;
        private readonly IETagChecker etagChecker;

        public CommandScheduler(
            IStore<TAggregate> store,
            IETagChecker etagChecker)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (etagChecker == null)
            {
                throw new ArgumentNullException("etagChecker");
            }
            this.store = store;
            this.etagChecker = etagChecker;
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
            if (scheduledCommand.Result is CommandDeduplicated)
            {
                return;
            }

            if (scheduledCommand.Command.CanBeDeliveredDuringScheduling() && scheduledCommand.IsDue())
            {
                if (!await PreconditionHasBeenMet(scheduledCommand))
                {
                    CommandScheduler.DeliverIfPreconditionIsMetSoon(
                        scheduledCommand,
                        Configuration.Current);
                }
                else
                {
                    // resolve the command scheduler so that delivery goes through the whole pipeline
                    await Configuration.Current.CommandScheduler<TAggregate>().Deliver(scheduledCommand);
                    return;
                }
            }

            if (scheduledCommand.Result == null)
            {
                throw new NotSupportedException("Deferred scheduling is not supported by the current command scheduler pipeline configuration.");
            }
        }

        /// <summary>
        /// Delivers the specified scheduled command to the target.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command to be applied to the target.</param>
        /// <returns>
        /// A task that is complete when the command has been applied.
        /// </returns>
        /// <remarks>
        /// The scheduler will apply the command and save it, potentially triggering additional consequences.
        /// </remarks>
        public virtual async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.Result is CommandDelivered)
            {
                return;
            }

            await store.ApplyScheduledCommand(scheduledCommand, etagChecker);
        }

        /// <summary>
        /// Verifies that the command precondition has been met.
        /// </summary>
        private async Task<bool> PreconditionHasBeenMet(IScheduledCommand scheduledCommand)
        {
            return await etagChecker.IsPreconditionSatisfied(scheduledCommand);
        }
    }
}