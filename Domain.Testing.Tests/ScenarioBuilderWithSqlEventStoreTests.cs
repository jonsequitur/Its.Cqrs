// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Tests;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ScenarioBuilderWithSqlEventStoreTests : ScenarioBuilderTests
    {
        private EventStoreDbTest eventStoreDbTest;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            eventStoreDbTest = new EventStoreDbTest();
            eventStoreDbTest.SetUp();
            RegisterForDisposal(Disposable.Create(() => eventStoreDbTest.TearDown()));
        }

        protected override ScenarioBuilder CreateScenarioBuilder()
        {
            CommandSchedulerDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";

            var scenarioBuilder = new ScenarioBuilder()
                .UseSqlEventStore()
                .UseSqlCommandScheduler();

            return scenarioBuilder;
        }
    }
}