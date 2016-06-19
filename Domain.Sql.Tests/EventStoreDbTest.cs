// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.Its.Domain.Tests.Infrastructure;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class EventStoreDbTest
    {
        public long HighestEventId;
        protected CompositeDisposable disposables;
        private bool classInitializeHasBeenCalled;

        public EventStoreDbTest()
        {
            Logging.Configure();

            Command<Order>.AuthorizeDefault = (order, command) => true;
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
        }

        protected virtual void AfterClassIsInitialized()
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            var startTime = DateTime.Now;

            disposables = new CompositeDisposable
            {
                Disposable.Create(() =>
                {
                    Console.WriteLine("\ntest took: " + (DateTimeOffset.Now - startTime).TotalSeconds + "s");

#if DEBUG
                    Console.WriteLine("\noutstanding AppLocks: " + AppLock.Active.Count);
#endif
                })
            };

            HighestEventId = EventStoreDbContext()
                .DisposeAfter(db => GetHighestEventId(db));

            if (!classInitializeHasBeenCalled)
            {
                classInitializeHasBeenCalled = true;
                AfterClassIsInitialized();
            }
        }

        protected static long GetHighestEventId(EventStoreDbContext db)
        {
            return db.Events.Max<StorableEvent, long?>(e => e.Id) ?? 0;
        }

        [TearDown]
        public virtual void TearDown()
        {
            disposables.IfNotNull()
                       .ThenDo(d => d.Dispose());
        }

        public ReadModelCatchup CreateReadModelCatchup(params object[] projectors)
        {
            var catchup = new ReadModelCatchup(
                eventStoreDbContext: () => EventStoreDbContext(),
                readModelDbContext: () => ReadModelDbContext(),
                startAtEventId: HighestEventId + 1,
                projectors: projectors)
            {
                Name = "from " + (HighestEventId + 1)
            };
            disposables.Add(catchup);
            return catchup;
        }

        public ReadModelCatchup<T> CreateReadModelCatchup<T>(
            Func<EventStoreDbContext> eventStoreDbContext,
            params object[] projectors)
            where T : DbContext, new()
        {
            var catchup = new ReadModelCatchup<T>(
                eventStoreDbContext: eventStoreDbContext,
                readModelDbContext: () => new T(),
                startAtEventId: HighestEventId + 1,
                projectors: projectors)
            {
                Name = "from " + (HighestEventId + 1)
            };
            disposables.Add(catchup);
            return catchup;
        }

        public ReadModelCatchup<T> CreateReadModelCatchup<T>(params object[] projectors)
            where T : DbContext, new()
        {
            return CreateReadModelCatchup<T>(() => EventStoreDbContext(), projectors);
        }
    }
}
