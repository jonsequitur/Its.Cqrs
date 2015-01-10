// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Testing
{
    /// <summary>
    /// A virtual domain clock that can be used for testing time-dependent operations.
    /// </summary>
    [DebuggerStepThrough]
    public class VirtualClock :
        IClock,
        IObservable<DateTimeOffset>,
        IDisposable
    {
        private readonly Subject<DateTimeOffset> movements = new Subject<DateTimeOffset>();

        private VirtualClock(DateTimeOffset now)
        {
            Scheduler = new HistoricalScheduler(now);
        }

        private readonly HistoricalScheduler Scheduler;

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

        public void AdvanceTo(DateTimeOffset time)
        {
            Scheduler.AdvanceTo(time);
            movements.OnNext(Scheduler.Now);
        }

        public void AdvanceBy(TimeSpan time)
        {
            Scheduler.AdvanceBy(time);
            movements.OnNext(Scheduler.Now);
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
            var clock =
                Clock.Current
                     .IfTypeIs<VirtualClock>()
                     .Else(() => CommandContext.Current.Root.Clock as VirtualClock);

            if (clock == null)
            {
                throw new InvalidOperationException("In-memory command scheduling can only be performed when a VirtualClock is active.");
            }

            return clock.Scheduler.Schedule(scheduledCommand, dueTime, func);
        }

        public override string ToString()
        {
            return GetType() + ": " + Now().ToString("O");
        }
    }
}
