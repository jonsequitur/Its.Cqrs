// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class SqlEventSourcedRepositoryMigrationTests : EventMigrationTests
    {
        protected override IEventSourcedRepository<Order> CreateRepository()
        {
            return new SqlEventSourcedRepository<Order>(
                createEventStoreDbContext: () => EventStoreDbContext());
        }
    }
}