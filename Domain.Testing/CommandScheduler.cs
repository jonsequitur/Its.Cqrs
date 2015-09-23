// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    public static class CommandScheduler
    {
        /// <summary>
        /// Allows awaiting delivery of all commands that are currently due on the command scheduler.
        /// </summary>
        /// <param name="scheduler">The command scheduler.</param>
        /// <param name="clockName">The name of the clock on which the commands are scheduled.</param>
        [Obsolete("This method will be removed in a future version. Use CommandScheduler.CommandSchedulerDone instead.")]
        public static async Task Done(
            this ISchedulerClockTrigger scheduler,
            string clockName = null)
        {
            clockName = clockName ?? SqlCommandScheduler.DefaultClockName;

            using (var db = new CommandSchedulerDbContext())
            {
                var now = Clock.Latest(Clock.Current, db.Clocks.Single(c => c.Name == clockName)).Now();
                await scheduler.AdvanceClock(clockName, now);
            }
        }

        /// <summary>
        /// Returns a <see cref="Task" /> that allows awaiting the completion of commands currently scheduled and due on the configured command scheduler.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds to wait for the scheduler to complete. If it hasn't completed by the specified time, a <see cref="TimeoutException" /> is thrown.</param>
        /// <returns></returns>
        public static Task CommandSchedulerDone(
            this Configuration configuration,
            int timeoutInMilliseconds = 5000)
        {
            var virtualClockDone = Clock.Current.IfTypeIs<VirtualClock>()
                                        .Then(c => c.Done())
                                        .Else(() => Task.FromResult(Unit.Default));

            var noCommandsInPipeline = configuration.Container
                                                    .Resolve<Domain.CommandScheduler.CommandsInPipeline>()
                                                    .Done();

            return Task.WhenAll(virtualClockDone, noCommandsInPipeline)
                       .TimeoutAfter(TimeSpan.FromMilliseconds(timeoutInMilliseconds));
        }

        internal static ScheduledCommandInterceptor<TAggregate> WithInMemoryDeferredScheduling<TAggregate>(Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            return async (command, next) =>
            {
                if (command.Result == null)
                {
                    var clock = Clock.Current;

                    command.Result = new CommandScheduled(command, clock);

                    // deliver the command immediately if appropriate
                    if (command.IsDue())
                    {
                        var preconditionVerifier = configuration.Container.Resolve<ICommandPreconditionVerifier>();

                        // sometimes the command depends on a precondition event that hasn't been saved
                        if (!await preconditionVerifier.IsPreconditionSatisfied(command))
                        {
                            Domain.CommandScheduler.DeliverIfPreconditionIsSatisfiedSoon(command, configuration);
                        }
                        else
                        {
                            await Domain.CommandScheduler.DeliverImmediatelyOnConfiguredScheduler(command, configuration);
                            return;
                        }
                    }

                    if (!(command.Result is CommandDelivered))
                    {
                        VirtualClock.Schedule(
                            command,
                            command.DueTime ?? Clock.Now().AddTicks(1),
                            (s, c) =>
                            {
                                Domain.CommandScheduler.DeliverImmediatelyOnConfiguredScheduler(c, configuration).Wait();
                                return Disposable.Empty;
                            });
                    }
                }

                await next(command);
            };
        }
    }
}