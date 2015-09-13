// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Diagnostics;
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

                    foreach (var scheduledCommand in commands)
                    {
                        await scheduler.Trigger(
                            scheduledCommand,
                            new SchedulerAdvancedResult(),
                            db);
                    }

                    await Task.Delay(400);
                }
            }
        }

        public static Configuration TraceCommandsFor<TAggregate>(
            this Configuration configuration)
            where TAggregate : class, IEventSourced
        {
            // resolve and register so there's only a single instance registered at any given time
            var inPipeline = configuration.Container.Resolve<CommandsInPipeline>();
            configuration.Container.Register(c => inPipeline);

            return configuration.AddToCommandSchedulerPipeline<TAggregate>(
                schedule: async (command, next) =>
                {
                    inPipeline.Add(command);
                    await next(command);
                    Trace.WriteLine(Clock.Now() + " [Schedule] " + command);
                },
                deliver: async (command, next) =>
                {
                    await next(command);
                    Trace.WriteLine(Clock.Now() + " [Deliver] " + command);
                    inPipeline.Remove(command);
                });
        }

        public static Task CommandSchedulerDone(
            this Configuration configuration,
            int timeoutInMilliseconds = 5000)
        {
            var virtualClockDone = Clock.Current.IfTypeIs<VirtualClock>()
                                        .Then(c => c.Done())
                                        .Else(() => Task.FromResult(Unit.Default));

            var noCommandsInPipeline = configuration.Container
                                                    .Resolve<CommandsInPipeline>()
                                                    .Done();

            return Task.WhenAll(virtualClockDone, noCommandsInPipeline)
                       .TimeoutAfter(TimeSpan.FromMilliseconds(timeoutInMilliseconds));
        }

        internal class CommandsInPipeline
        {
            private readonly ConcurrentDictionary<IScheduledCommand, DateTimeOffset> commands = new ConcurrentDictionary<IScheduledCommand, DateTimeOffset>();

            public void Add(IScheduledCommand command)
            {
                var now = Clock.Now();
                commands.AddOrUpdate(
                    command,
                    now,
                    (c, t) => now);
            }

            public void Remove(IScheduledCommand command)
            {
                DateTimeOffset _;
                commands.TryRemove(command, out _);
            }

            public async Task Done()
            {
                while (true)
                {
                    var now = Clock.Current;
                    if (!commands.Keys.Any(c => c.IsDue(now)))
                    {
                        return;
                    }
                }
            }
        }

        internal static ScheduledCommandInterceptor<TAggregate> WithInMemoryDeferredScheduling<TAggregate>(Configuration configuration)
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