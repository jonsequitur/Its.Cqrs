// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Tests.Infrastructure;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class EventStoreDbTest 
    {
        private static bool databasesInitialized;
        private static readonly object lockObj = new object();
        public long HighestEventId;
        private CompositeDisposable disposables;
        private bool classInitializeHasBeenCalled;

        private static void SetConnectionStrings()
        {
            EventStoreDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsEventStore";
            ReadModelDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsReadModels";
            CommandSchedulerDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";
        }

        static EventStoreDbTest()
        {
            TaskScheduler.UnobservedTaskException += (sender, args) => Console.WriteLine("Unobserved exception: " + args.Exception);
        }

        public EventStoreDbTest()
        {
            Logging.Configure();

            SetConnectionStrings();

            Command<Order>.AuthorizeDefault = (order, command) => true;
            Command<CustomerAccount>.AuthorizeDefault = (order, command) => true;
         
            lock (lockObj)
            {
                if (databasesInitialized)
                {
                    return;
                }

                using (var eventStore = new EventStoreDbContext())
                {
                    new EventStoreDatabaseInitializer<EventStoreDbContext>().InitializeDatabase(eventStore);
                }  
                using (var db = new CommandSchedulerDbContext())
                {
                    new CommandSchedulerDatabaseInitializer().InitializeDatabase(db);
                }
                using (var eventStore = new OtherEventStoreDbContext())
                {
                    new EventStoreDatabaseInitializer<OtherEventStoreDbContext>().InitializeDatabase(eventStore);
                }
                using (var db = new ReadModelDbContext())
                {
                    new ReadModelDatabaseInitializer<ReadModelDbContext>().InitializeDatabase(db);
                }
                using (var db = new ReadModels1DbContext())
                {
                    new ReadModelDatabaseInitializer<ReadModels1DbContext>().InitializeDatabase(db);
                }
                using (var db = new ReadModels2DbContext())
                {
                    new ReadModelDatabaseInitializer<ReadModels2DbContext>().InitializeDatabase(db);
                }

                databasesInitialized = true;
            }
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

            HighestEventId = new EventStoreDbContext().DisposeAfter(db => GetHighestEventId(db));

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

        public virtual CatchupWrapper CreateReadModelCatchup(params object[] projectors)
        {
            var startAtEventId = HighestEventId + 1;
            var catchup = new ReadModelCatchup(projectors)
            {
                StartAtEventId = startAtEventId,
                Name = "from " + startAtEventId
            };
            disposables.Add(catchup);
            return new CatchupWrapper<ReadModelDbContext>(catchup);
        }

        public virtual CatchupWrapper CreateReadModelCatchup<T>(params object[] projectors)
            where T : DbContext, new()
        {
            var startAtEventId = HighestEventId + 1;
            var catchup = new ReadModelCatchup<T>(projectors)
            {
                StartAtEventId = startAtEventId,
                Name = "from " + startAtEventId
            };
            disposables.Add(catchup);

            return new CatchupWrapper<T>(catchup);
        }
    }
}
