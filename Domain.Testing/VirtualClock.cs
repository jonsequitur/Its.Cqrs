// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// A virtual domain clock that can be used for testing time-dependent operations.
    /// </summary>
    public class VirtualClock :
        IClock,
        IDisposable,
        IObservable<DateTimeOffset>
    {
        private readonly Subject<DateTimeOffset> movements = new Subject<DateTimeOffset>();
        private readonly RxScheduler Scheduler;
        private readonly ConcurrentHashSet<IClock> schedulerClocks = new ConcurrentHashSet<IClock>();
        private string creatorMemberName;
        private string creatorFilePath;

        private VirtualClock(DateTimeOffset now)
        {
            Scheduler = new RxScheduler(now);
        }

        /// <summary>
        /// Gets the current clock as a <see cref="VirtualClock" />. If the current clock is not a <see cref="VirtualClock" />, it throws.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        /// <exception cref="System.InvalidOperationException">Clock.Current must be a VirtualClock in order to use this method.</exception>
        public static VirtualClock Current
        {
            get
            {
                var clock = Clock.Current as VirtualClock;
                if (clock == null)
                {
                    throw new InvalidOperationException("Clock.Current must be a VirtualClock in order to use this method.");
                }
                return clock;
            }
        }

        /// <summary>
        /// Gets the current time.
        /// </summary>
        public DateTimeOffset Now() => Scheduler.Clock;

        /// <summary>
        /// Advances the clock to the specified time.
        /// </summary>
        public void AdvanceTo(DateTimeOffset time)
        {
            Scheduler.AdvanceTo(time);
            movements.OnNext(Scheduler.Now);
            WaitForScheduler();
        }

        /// <summary>
        /// Advances the clock by the specified amount of time.
        /// </summary>
        public void AdvanceBy(TimeSpan time)
        {
            Scheduler.AdvanceBy(time);
            movements.OnNext(Scheduler.Now);
            WaitForScheduler();
        }

        private void WaitForScheduler()
        {
            Scheduler.Done()
                     .TimeoutAfter(Scenario.DefaultTimeout())
                     .Wait();

            var configuration = Configuration.Current;

            var commandsInPipeline = configuration.Container.Resolve<CommandsInPipeline>();

            do
            {
                var pendingCommands = commandsInPipeline
                    .Select(c => c.Result)
                    .OfType<CommandScheduled>()
                    .ToArray();

                if (pendingCommands.Any())
                {
                    var namesOfClocksWithPendingCommands = pendingCommands
                        .Select(s => s.Clock)
                        .OfType<Sql.CommandScheduler.Clock>()
                        .Select(c => c.Name)
                        .Distinct()
                        .ToArray();

                    if (namesOfClocksWithPendingCommands.Any())
                    {
                        var clockTrigger = configuration.SchedulerClockTrigger();

                        var appliedCommands = namesOfClocksWithPendingCommands
                            .Select(clockName => clockTrigger.AdvanceClock(clockName,
                                                                           Now(),
                                                                           q => q.Take(1))
                                                             .TimeoutAfter(Scenario.DefaultTimeout())
                                                             .Result)
                            .SelectMany(result => result.SuccessfulCommands
                                                        .Cast<ScheduledCommandResult>()
                                                        .Concat(result.FailedCommands))
                            .ToArray();

                        if (!appliedCommands.Any())
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            } while (true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose() => Clock.Reset();

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        /// <param name="observer">The object that is to receive notifications.</param>
        public IDisposable Subscribe(IObserver<DateTimeOffset> observer) => movements.Subscribe(observer);

        /// <summary>
        /// Replaces the domain clock with a virtual clock that can be used to control the current time.
        /// </summary>
        /// <param name="now">The time to which the virtual clock is set.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">You must dispose the current VirtualClock before starting another.</exception>
        /// <param name="caller">The caller.</param>
        /// <param name="callerFilePath">The caller file path.</param>
        public static VirtualClock Start(
            DateTimeOffset? now = null,
            [CallerMemberName] string caller = null,
            [CallerFilePath] string callerFilePath = null)
        {
            var clock = Clock.Current as VirtualClock;
            if (clock != null)
            {
                throw new InvalidOperationException($"You must dispose the current VirtualClock (created by {clock.creatorMemberName} [{clock.creatorFilePath}]) before starting another.");
            }

            Configuration.Current.EnsureCommandSchedulerPipelineTrackerIsInitialized();

            var virtualClock = new VirtualClock(now ?? DateTimeOffset.Now)
            {
                creatorMemberName = caller,
                creatorFilePath = callerFilePath
            };

            Clock.Current = virtualClock;

            return virtualClock;
        }

        internal static IDisposable Schedule<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            DateTimeOffset dueTime,
            Func<IScheduler, IScheduledCommand<TAggregate>, IDisposable> func)
        {
            var scheduler = Clock.Current
                                 .IfTypeIs<VirtualClock>()
                                 .Then(c => (IScheduler) c.Scheduler)
                                 .Else(() => CurrentThreadScheduler.Instance);

            return scheduler.Schedule(scheduledCommand, dueTime, func);
        }

        /// <summary>
        /// Gets a task that completes when all currently-due command scheduler work is done.
        /// </summary>
        public async Task Done() => await Scheduler.Done();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString() => $"{GetType()}: {Now():O}";

        private class RxScheduler : HistoricalScheduler
        {
            private readonly IDictionary<IScheduledCommand, DateTimeOffset> pending = new ConcurrentDictionary<IScheduledCommand, DateTimeOffset>();

            private readonly ManualResetEventSlim resetEvent = new ManualResetEventSlim(true);

            public RxScheduler(DateTimeOffset initialClock) : base(initialClock)
            {
            }

            public override IDisposable ScheduleAbsolute<TState>(
                TState state,
                DateTimeOffset dueTime,
                Func<IScheduler, TState, IDisposable> deliver)
            {
                resetEvent.Reset();

                pending.Add((IScheduledCommand) state, dueTime);

                var schedule = base.ScheduleAbsolute(state, dueTime, (scheduler, command) =>
                {
                    var cancel = deliver(scheduler, command);

                    var scheduledCommand = (IScheduledCommand) command;

                    var failed = scheduledCommand.Result.IfTypeIs<CommandFailed>()
                                                 .ElseDefault();

                    pending.Remove(scheduledCommand);

                    if (failed != null && failed.WillBeRetried)
                    {
                        var retryAfter = failed.RetryAfter ??
                                         TimeSpan.FromTicks(1);

                        var clone = CreateNewScheduledCommandFrom(
                            (dynamic) command,
                            retryAfter);

                        // scheduling the command will add it back to pending
                        scheduler.Schedule(
                            clone,
                            retryAfter,
                            deliver);
                    }

                    resetEvent.Set();

                    return cancel;
                });

                return schedule;
            }

            private IScheduledCommand<T> CreateNewScheduledCommandFrom<T>(
                IScheduledCommand<T> scheduledCommand,
                TimeSpan retryAfter)
                where T : class
            {
                return new ScheduledCommand<T>(
                    scheduledCommand.Command,
                    scheduledCommand.TargetId,
                    Domain.Clock.Current.Now() + retryAfter)
                {
                    NumberOfPreviousAttempts = scheduledCommand.NumberOfPreviousAttempts + 1,
                    Clock = scheduledCommand.Clock
                };
            }

            public async Task Done()
            {
                await Task.Yield();

                while (CommandsAreDue)
                {
                    resetEvent.Wait();
                }
            }

            private bool CommandsAreDue => pending.Any(p => p.Value <= Now);
        }

        internal void OnAdvanceTriggerSchedulerClock(IClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }
            schedulerClocks.Add(clock);
        }
    }
}