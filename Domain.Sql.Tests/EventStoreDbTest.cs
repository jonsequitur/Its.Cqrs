// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity;
using System.Linq.Expressions;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NCrunch.Framework;
using NUnit.Framework;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    [DisableCommandAuthorization]
    [UseSqlEventStore]
    public abstract class EventStoreDbTest
    {
        protected long HighestEventId;

        [SetUp]
        public virtual void SetUp() =>
            HighestEventId = EventStoreDbContext()
                                 .DisposeAfter(db => db.HighestEventId());

        public ReadModelCatchup CreateReadModelCatchup(params object[] projectors)
        {
            var catchup = new ReadModelCatchup(
                eventStoreDbContext: () => EventStoreDbContext(),
                readModelDbContext: () => ReadModelDbContext(),
                startAtEventId: HighestEventId + 1,
                projectors: projectors)
            {
                Name = $"from {HighestEventId + 1}"
            };
            Configuration.Current.RegisterForDisposal(catchup);
            return catchup;
        }

        public ReadModelCatchup CreateReadModelCatchup(
            Expression<Func<StorableEvent, bool>> filter = null,
            int batchSize = 10000,
            long? startAtEventId = null,
            params object[] projectors)
        {
            startAtEventId = startAtEventId ??
                             HighestEventId + 1;

            var catchup = new ReadModelCatchup(
                eventStoreDbContext: () => EventStoreDbContext(),
                readModelDbContext: () => ReadModelDbContext(),
                startAtEventId: startAtEventId.Value,
                projectors: projectors,
                batchSize: batchSize,
                filter: filter)
            {
                Name = $"from {startAtEventId}"
            };
            Configuration.Current.RegisterForDisposal(catchup);
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
                              Name = $"from {HighestEventId + 1}"
                          };
            Configuration.Current.RegisterForDisposal(catchup);
            return catchup;
        }
        
        public ReadModelCatchup<T> CreateReadModelCatchup<T>(params object[] projectors)
            where T : DbContext, new() =>
                CreateReadModelCatchup<T>(() => EventStoreDbContext(), projectors);
    }
}
