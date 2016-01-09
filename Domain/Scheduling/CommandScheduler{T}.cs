// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A basic command scheduler implementation that can be used as the basis for composing command scheduling behaviors.
    /// </summary>
    /// <typeparam name="TTarget">The type of the command target.</typeparam>
    internal class CommandScheduler<TTarget> : ICommandScheduler<TTarget> 
        where TTarget : class
    {
        private readonly ICommandApplier<TTarget> commandApplier;
        private readonly ICommandPreconditionVerifier preconditionVerifier;

        public CommandScheduler(
            ICommandApplier<TTarget> commandApplier,
            ICommandPreconditionVerifier preconditionVerifier)
        {
            if (commandApplier == null)
            {
                throw new ArgumentNullException("commandApplier");
            }
            this.commandApplier = commandApplier;
            this.preconditionVerifier = preconditionVerifier;
        }

        /// <summary>
        /// Schedules the specified command.
        /// </summary>
        /// <param name="scheduledCommand">The scheduled command.</param>
        /// <returns>
        /// A task that is complete when the command has been successfully scheduled.
        /// </returns>
        /// <exception cref="System.NotSupportedException">Non-immediate scheduling is not supported.</exception>
        public virtual async Task Schedule(IScheduledCommand<TTarget> scheduledCommand)
        {
            if (scheduledCommand.Command.CanBeDeliveredDuringScheduling() && scheduledCommand.IsDue())
            {
                if (!await VerifyPrecondition(scheduledCommand))
                {
                    CommandScheduler.DeliverIfPreconditionIsSatisfiedSoon(
                        scheduledCommand,
                        Configuration.Current);
                }
                else
                {
                    // resolve the command scheduler so that delivery goes through the whole pipeline
                    await Configuration.Current.CommandScheduler<TTarget>().Deliver(scheduledCommand);
                    return;
                }
            }

            if (scheduledCommand.Result == null)
            {
                throw new NotSupportedException("Deferred scheduling is not supported.");
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
        public virtual async Task Deliver(IScheduledCommand<TTarget> scheduledCommand)
        {
            await commandApplier.ApplyScheduledCommand(scheduledCommand);
        }

        /// <summary>
        /// Verifies that the command precondition has been met.
        /// </summary>
        private async Task<bool> VerifyPrecondition(IScheduledCommand scheduledCommand)
        {
            return await preconditionVerifier.IsPreconditionSatisfied(scheduledCommand);
        }
    }
}