// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
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
        public DateTimeOffset Now()
        {
            return Scheduler.Clock;
        }

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

            if (configuration.IsUsingLegacySqlCommandScheduling())
            {
                if (schedulerClocks.Any())
                {
                    foreach (var clock in schedulerClocks.OfType<Sql.CommandScheduler.Clock>())
                    {
                        configuration.SqlCommandScheduler()
                                     .AdvanceClock(clock.Name, Clock.Now())
                                     .TimeoutAfter(Scenario.DefaultTimeout())
                                     .Wait();
                    }
                }
            }
            else if (configuration.IsUsingCommandSchedulerPipeline())
            {
                var commandsInPipeline = configuration.Container.Resolve<CommandsInPipeline>();

                var sqlSchedulerClocks = commandsInPipeline
                    .Select(c => c.Result)
                    .OfType<CommandScheduled>()
                    .Select(s => s.Clock)
                    .OfType<Sql.CommandScheduler.Clock>()
                    .Distinct()
                    .ToArray();

                if (sqlSchedulerClocks.Any())
                {
                    var clockTrigger = configuration.Container
                                                    .Resolve<ISchedulerClockTrigger>();

                    sqlSchedulerClocks.ForEach(c =>
                    {
                        clockTrigger
                            .AdvanceClock(c.Name, Clock.Now())
                            .TimeoutAfter(Scenario.DefaultTimeout())
                            .Wait();
                    });
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Clock.Reset();
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <returns>
        /// A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.
        /// </returns>
        /// <param name="observer">The object that is to receive notifications.</param>
        public IDisposable Subscribe(IObserver<DateTimeOffset> observer)
        {
            return movements.Subscribe(observer);
        }

        /// <summary>
        /// Replaces the domain clock with a virtual clock that can be used to control the current time.
        /// </summary>
        /// <param name="now">The time to which the virtual clock is set.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">You must dispose the current VirtualClock before starting another.</exception>
        public static VirtualClock Start(DateTimeOffset? now = null)
        {
            if (Clock.Current is VirtualClock)
            {
                throw new InvalidOperationException("You must dispose the current VirtualClock before starting another.");
            }

            Configuration.Current.EnsureCommandSchedulerPipelineTrackerIsInitialized();

            var virtualClock = new VirtualClock(now ?? DateTimeOffset.Now);
            Clock.Current = virtualClock;

            return virtualClock;
        }

        internal static IDisposable Schedule<TAggregate>(
            IScheduledCommand<TAggregate> scheduledCommand,
            DateTimeOffset dueTime,
            Func<IScheduler, IScheduledCommand<TAggregate>, IDisposable> func)
            where TAggregate : IEventSourced
        {
            var scheduler = Clock.Current.IfTypeIs<VirtualClock>()
                                 .Then(c => (IScheduler) c.Scheduler)
                                 .Else(() => TaskPoolScheduler.Default);

            return scheduler.Schedule(scheduledCommand, dueTime, func);
        }

        public async Task Done()
        {
            await Scheduler.Done();
        }

        public override string ToString()
        {
            return GetType() + ": " + Now().ToString("O");
        }

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
                Func<IScheduler, TState, IDisposable> action)
            {
                resetEvent.Reset();

                pending.Add((IScheduledCommand) state, dueTime);

                var schedule = base.ScheduleAbsolute(state, dueTime, (scheduler, command) =>
                {
                    var cancel = action(scheduler, command);

                    pending.Remove((IScheduledCommand) command);

                    resetEvent.Set();

                    return cancel;
                });

                return schedule;
            }

            public async Task Done()
            {
                await Task.Yield();

                while (CommandsAreDue)
                {
                    resetEvent.Wait();
                }
            }

            private bool CommandsAreDue
            {
                get
                {
                    return pending.Any(p => p.Value <= Now);
                }
            }
        }

        internal void OnAdvanceTriggerSchedulerClock(IClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException("clock");
            }
            schedulerClocks.Add(clock);
        }
    }
}