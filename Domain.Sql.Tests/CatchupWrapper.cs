// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Concurrency;

namespace Microsoft.Its.Domain.Sql.Tests
{
    /// <summary>
    ///     A test shim for different catchup implementations that don't share common base types.
    /// </summary>
    public abstract class CatchupWrapper : IDisposable
    {
        public abstract Func<EventStoreDbContext> CreateEventStoreDbContext { get; set; }
        public abstract Func<DbContext> CreateReadModelDbContext { get; set; }

        public abstract IEventBus EventBus { get; }
        public abstract string Name { get; set; }
        public abstract IObservable<ReadModelCatchupStatus> Progress { get; }

        protected abstract dynamic InnerCatchup { get; }

        public static IObservable<ReadModelCatchupStatus> SingleBatchAsync(
            params CatchupWrapper[] catchups)
        {
            var wrappedCatchups = catchups.Select(c => c.InnerCatchup)
                                          .Cast<ReadModelCatchup<ReadModelDbContext>>()
                                          .ToArray();
            return Catchup.SingleBatchAsync(wrappedCatchups);
        }

        public abstract CatchupWrapper PollEventStore(TimeSpan? interval = null,
                                                      IScheduler scheduler = null);

        public abstract ReadModelCatchupResult Run();

        public abstract IObservable<ReadModelCatchupStatus> SingleBatchAsync(IScheduler scheduler = null);

        public abstract void Dispose();
    }

    public class CatchupWrapper<T> : CatchupWrapper
        where T : DbContext, new()
    {
        private readonly ReadModelCatchup<T> catchup;

        public CatchupWrapper(ReadModelCatchup<T> catchup)
        {
            if (catchup == null)
            {
                throw new ArgumentNullException("catchup");
            }
            this.catchup = catchup;
        }

        public override Func<EventStoreDbContext> CreateEventStoreDbContext
        {
            get
            {
                return catchup.CreateEventStoreDbContext;
            }
            set
            {
                catchup.CreateEventStoreDbContext = value;
            }
        }

        public override Func<DbContext> CreateReadModelDbContext
        {
            get
            {
                return catchup.CreateReadModelDbContext;
            }
            set
            {
                catchup.CreateReadModelDbContext = value;
            }
        }

        public override IEventBus EventBus
        {
            get
            {
                return catchup.EventBus;
            }
        }

        public override string Name
        {
            get
            {
                return catchup.Name;
            }
            set
            {
                catchup.Name = value;
            }
        }

        public override IObservable<ReadModelCatchupStatus> Progress
        {
            get
            {
                return catchup.Progress;
            }
        }

        protected override dynamic InnerCatchup
        {
            get
            {
                return catchup;
            }
        }

        public override void Dispose()
        {
            catchup.Dispose();
        }

        public override CatchupWrapper PollEventStore(
            TimeSpan? interval = null,
            IScheduler scheduler = null)
        {
            catchup.PollEventStore(interval, scheduler);
            return this;
        }

        public override ReadModelCatchupResult Run()
        {
            return catchup.Run();
        }

        public override IObservable<ReadModelCatchupStatus> SingleBatchAsync(
            IScheduler scheduler = null)
        {
            return catchup.SingleBatchAsync(scheduler);
        }
    }
}
