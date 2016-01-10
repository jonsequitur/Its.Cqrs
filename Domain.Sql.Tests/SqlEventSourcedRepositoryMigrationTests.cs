// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Entity;
using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Sample.Domain.Ordering;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlEventSourcedRepositoryMigrationTests : EventMigrationTests
    {
        static SqlEventSourcedRepositoryMigrationTests()
        {
            EventStoreDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsEventStore";
            Database.SetInitializer(new EventStoreDatabaseInitializer<EventStoreDbContext>());
        }

        protected override IEventSourcedRepository<Order> CreateRepository()
        {
            return new SqlEventSourcedRepository<Order>();
        }
    }
}