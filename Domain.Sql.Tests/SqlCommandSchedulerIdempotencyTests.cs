// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public abstract class SqlCommandSchedulerIdempotencyTests : CommandSchedulerIdempotencyTests
    {
        protected EventStoreDbTest eventStoreDbTest;
        protected string clockName;

        protected override void Configure(Configuration configuration)
        {
            clockName = Any.CamelCaseName();

            configuration
                .UseDependency<GetClockName>(c => e => clockName)
                .UseSqlEventStore(c => c.UseConnectionString(TestDatabases.EventStore.ConnectionString))
                .UseSqlStorageForScheduledCommands(c => c.UseConnectionString(TestDatabases.CommandScheduler.ConnectionString));
        }
    }
}