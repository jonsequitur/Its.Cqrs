// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql
{
    /// <summary>
    /// Provides methods for working with read model catchups.
    /// </summary>
    public static class Catchup
    {
        /// <summary>
        /// Polls the event store and passes events to <see cref="ReadModelCatchup{TDbContext}.Run" /> when new events are found.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the database context used to access the read models.</typeparam>
        /// <param name="readModelCatchup">The read model catchup instance to run.</param>
        /// <param name="interval">The interval at which polling occurs.</param>
        /// <param name="scheduler">The scheduler on which work is scheduled.</param>
        public static ReadModelCatchup<TDbContext> PollEventStore<TDbContext>(
            this ReadModelCatchup<TDbContext> readModelCatchup,
            TimeSpan? interval = null,
            IScheduler scheduler = null)
            where TDbContext : DbContext
        {
            interval = interval ?? TimeSpan.FromSeconds(5);
            scheduler = scheduler ?? Scheduler.Default;
            return readModelCatchup.PollEventStore(Observable.Interval(interval.Value, scheduler).Select(_ => Unit.Default));
        }

        /// <summary>
        /// Polls the event store and passes events to <see cref="ReadModelCatchup{TDbContext}.Run" /> when new events are found.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the database context used to access the read models.</typeparam>
        /// <param name="readModelCatchup">The read model catchup instance to run.</param>
        /// <param name="timer">An observable sequence that triggers event store polling.</param>
        public static ReadModelCatchup<TDbContext> PollEventStore<TDbContext>(
            this ReadModelCatchup<TDbContext> readModelCatchup,
            IObservable<Unit> timer)
            where TDbContext : DbContext
        {
            // produce a series of pings every time a) the timer elapses or b) a poll was just completed and there are still additional events to be processed
            // timer:                     o--------o--------o--------o--------o--------o--------o
            //                            |        |        |        |        |        |        |                                              
            // previous run completed:    --------------------------------o----------------------
            //                            |        |        |        |    |   |        |        | 
            // merged:                    o--------o--------o--------o----o---o--------o--------o
            IObservable<Unit> pollingPings = readModelCatchup
                .Progress
                .Where(status =>
                {
                    // should we poll again? polling again immediately reduces potential latency by not waiting until the interval-baed polling happens again.  
                    if (status.IsEndOfBatch &&
                        status.BatchCount > 0)
                    {
                        using (var eventStore = Task.Run(readModelCatchup.CreateOpenEventStoreDbContext).Result)
                        {
                            if (eventStore.Events.Max(e => e.Id) > status.CurrentEventId)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                })
                .Select(_ => Unit.Default)
                .Merge(timer);

            readModelCatchup.RunWhen(pollingPings);

            return readModelCatchup;
        }

        /// <summary>
        /// Runs or observes a single catchup batch asynchronously.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the database context.</typeparam>
        /// <param name="catchup">The read model catchup.</param>
        /// <param name="scheduler">A scheduler on which to schedule the catchup run. If none is specified, the default scheduler is used.</param>
        /// <returns>An observable of the catchup batch's progress, whether it was triggered by the caller or was already running.</returns>
        public static IObservable<ReadModelCatchupStatus> SingleBatchAsync<TDbContext>(
            this ReadModelCatchup<TDbContext> catchup,
            IScheduler scheduler = null)
            where TDbContext : DbContext
        {
            scheduler = scheduler ?? Scheduler.Default;

            return Observable.Create<ReadModelCatchupStatus>(observer =>
            {
                var subscription = catchup.Progress.Subscribe(observer);

                var disposables = new CompositeDisposable { subscription };

                return scheduler.ScheduleAsync(async (sched, token) =>
                {
                    if (await catchup.Run() == ReadModelCatchupResult.CatchupAlreadyInProgress)
                    {
                        // monitor for the end of the batch
                        var currentBatch = catchup.Progress
                                                  .TakeWhile(s => !s.IsEndOfBatch)
                                                  .Subscribe(onNext: s => { },
                                                             onCompleted: observer.OnCompleted,
                                                             onError: e => observer.OnCompleted());

                        disposables.Add(currentBatch);
                    }
                    else
                    {
                        observer.OnCompleted();
                    }

                    return Task.FromResult(disposables);
                });
            });
        }

        /// <summary>
        /// Runs or observes a single catchup batch asynchronously for multiple catchup instances.
        /// </summary>
        public static IObservable<ReadModelCatchupStatus> SingleBatchAsync(
            params ReadModelCatchup<ReadModelDbContext>[] catchups)
        {
            return Observable.Create<ReadModelCatchupStatus>(observer =>
            {
                if (!catchups?.Any() ?? true)
                {
                    observer.OnCompleted();
                    return Disposable.Empty;
                }

                var completions = new RefCountDisposable(Disposable.Create(observer.OnCompleted));

                var subscriptions = new CompositeDisposable();

                catchups.ForEach(catchup =>
                {
                    var completion = completions.GetDisposable();

                    var sub = catchup.SingleBatchAsync()
                                     .Subscribe(onNext: observer.OnNext,
                                                onCompleted: completion.Dispose);

                    subscriptions.Add(sub);
                });

                completions.Dispose();

                return subscriptions;
            });
        }
    }
}
