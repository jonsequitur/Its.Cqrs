// Copyright ix c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A basic command scheduler implementation that can be used as the basis for composing command scheduling behaviors.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the command target.</typeparam>
    internal class CommandScheduler<TAggregate> :
        ICommandScheduler<TAggregate>,
        ICommandDeliverer<TAggregate>
        where TAggregate : class
    {
        private static readonly MethodInfo createCommandFailed = typeof(CommandFailed)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(m => m.Name == "Create");

        private readonly Func<IStore<TAggregate>> getStore;
        private readonly IETagChecker etagChecker;

        public CommandScheduler(
            Func<IStore<TAggregate>> getStore,
            IETagChecker etagChecker)
        {
            if (getStore == null)
            {
                throw new ArgumentNullException(nameof(getStore));
            }
            if (etagChecker == null)
            {
                throw new ArgumentNullException(nameof(etagChecker));
            }
            this.getStore = getStore;
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
        public async Task Schedule(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.Result is CommandDeduplicated)
            {
                return;
            }

            if (scheduledCommand.Command.CanBeDeliveredDuringScheduling() && 
                scheduledCommand.IsDue())
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
                    await Configuration.Current.CommandDeliverer<TAggregate>().Deliver(scheduledCommand);
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
        public async Task Deliver(IScheduledCommand<TAggregate> scheduledCommand)
        {
            if (scheduledCommand.Result is CommandDelivered)
            {
                return;
            }

            var store = getStore();

            await ApplyScheduledCommand(store, scheduledCommand);
        }

        private async Task ApplyScheduledCommand(
            IStore<TAggregate> store,
            IScheduledCommand<TAggregate> scheduled)
        {
            TAggregate aggregate = null;
            Exception exception;

            try
            {
                if (!await etagChecker.IsPreconditionSatisfied(scheduled))
                {
                    await FailScheduledCommand(store,
                        scheduled,
                        new PreconditionNotMetException(scheduled.DeliveryPrecondition));
                    return;
                }

                aggregate = await store.Get(scheduled.TargetId);

                var isConstructorCommand = scheduled.Command is ConstructorCommand<TAggregate>;

                if (aggregate == null)
                {
                    if (isConstructorCommand)
                    {
                        var ctor = typeof(TAggregate).GetConstructor(new[] { scheduled.Command.GetType() });

                        if (ctor == null)
                        {
                            throw new InvalidOperationException($"No constructor was found on type {typeof(TAggregate)} for constructor command {scheduled.Command}.");
                        }

                        aggregate = (TAggregate) ctor.Invoke(new[] { scheduled.Command });
                    }
                    else
                    {
                        throw new PreconditionNotMetException(
                            $"No {typeof(TAggregate).Name} was found with id {scheduled.TargetId} so the command could not be applied.");
                    }
                }
                else if (isConstructorCommand)
                {
                    throw new ConcurrencyException($"Command target having id {scheduled.TargetId} already exists");
                }
                else
                {
                    await aggregate.ApplyAsync(scheduled.Command);
                }

                await store.Put(aggregate);

                scheduled.Result = new CommandSucceeded(scheduled);

                return;
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            await FailScheduledCommand(store, scheduled, exception, aggregate);
        }

        private static async Task FailScheduledCommand(
            IStore<TAggregate> store,
            IScheduledCommand<TAggregate> scheduled,
            Exception exception = null,
            TAggregate aggregate = null)
        {
            var failure = (CommandFailed) createCommandFailed
                                              .MakeGenericMethod(scheduled.Command.GetType())
                                              .Invoke(null, new object[] { scheduled.Command, scheduled, exception });

            if (aggregate != null)
            {
                var scheduledCommandOfT = scheduled.Command as Command<TAggregate>;
                if (scheduledCommandOfT != null &&
                    scheduledCommandOfT.Handler != null)
                {
                    // re-retrieve the command target so that it's not in its an invalid state
                    aggregate = await store.Get(scheduled.TargetId);

                    await scheduledCommandOfT.Handler
                        .HandleScheduledCommandException((dynamic) aggregate,
                            (dynamic) failure);

                    await store.Put(aggregate);
                }

                if (exception is ConcurrencyException &&
                    scheduled.Command is ConstructorCommand<TAggregate>)
                {
                    // the aggregate has already been created, so this command will never succeed and is redundant.
                    // this may result from redelivery of a constructor command.
                    failure.Cancel();
                    scheduled.Result = failure;
                    return;
                }
            }

            if (failure.IsRetryableByDefault() &&
                failure.CommandHandlerDidNotSpecifyRetry())
            {
                failure.Retry();
            }

            scheduled.Result = failure;
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