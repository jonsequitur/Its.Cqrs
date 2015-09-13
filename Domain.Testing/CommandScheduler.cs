// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Testing
{
    public static class CommandScheduler
    {
        /// <summary>
        /// Allows awaiting delivery of all commands that are currently due on the command scheduler.
        /// </summary>
        /// <param name="scheduler">The command scheduler.</param>
        /// <param name="clockName">The name of the clock on which the commands are scheduled.</param>
        /// <returns></returns>
        public static async Task Done(
            this ISchedulerClockTrigger scheduler,
            string clockName = null)
        {
            clockName = clockName ?? SqlCommandScheduler.DefaultClockName;

            for (var i = 0; i < 10; i++)
            {
                using (var db = new CommandSchedulerDbContext())
                {
                    var due = db.ScheduledCommands
                                .Due()
                                .Where(c => c.Clock.Name == clockName);

                    if (!await due.AnyAsync())
                    {
                        return;
                    }

                    var commands = await due.ToArrayAsync();

                    Debug.WriteLine(string.Format("Triggering {0} commands", commands.Count()));

                    foreach (var scheduledCommand in commands)
                    {
                        Debug.WriteLine(string.Format("Triggering {0}:{1}", scheduledCommand.AggregateId, scheduledCommand.SequenceNumber));
                        await scheduler.Trigger(
                            scheduledCommand,
                            new SchedulerAdvancedResult(),
                            db);
                    }

                    await Task.Delay(400);
                }
            }

            Debug.WriteLine(string.Format("Done waiting for clock {0}", clockName));
        }

        public static Configuration TraceCommandsFor<TAggregate>(this Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            return configuration.AddToCommandSchedulerPipeline<TAggregate>(
                schedule: async (command, next) =>
                {
                    await next(command);
                    Trace.WriteLine(Clock.Now() + " [Schedule] " + command);
                },
                deliver: async (command, next) =>
                {
                    await next(command);
                    Trace.WriteLine(Clock.Now() + " [Deliver] " + command);
                });
        }

        internal static ScheduledCommandInterceptor<TAggregate> WithInMemoryDeferredScheduling<TAggregate>()
            where TAggregate : class, IEventSourced
        {
            return async (command, next) =>
            {
                if (command.Result == null)
                {
                    command.Result = new CommandScheduled(command);

                    // deliver the command immediately if appropriate
                    if (command.IsDue())
                    {
                        var preconditionVerifier = Configuration.Current.Container.Resolve<ICommandPreconditionVerifier>();

                        // sometimes the command depends on a precondition event that hasn't been saved
                        if (!await preconditionVerifier.IsPreconditionSatisfied(command))
                        {
                            Domain.CommandScheduler.DeliverIfPreconditionIsSatisfiedSoon(command);
                        }
                    }

                    if (!(command.Result is CommandDelivered))
                    {
                        VirtualClock.Schedule(
                            command,
                            command.DueTime ?? Clock.Now().AddTicks(1),
                            (s, c) =>
                            {
                                Domain.CommandScheduler.DeliverImmediatelyOnConfiguredScheduler(c).Wait();
                                return Disposable.Empty;
                            });
                    }
                }

                await next(command);
            };
        }
    }
}