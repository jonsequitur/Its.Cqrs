// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Tests;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ScenarioBuilderWithLegacySqlStorageTests : ScenarioBuilderTests
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

        public override bool UsesSqlStorage
        {
            get
            {
                return true;
            }
        }

        protected override ScenarioBuilder CreateScenarioBuilder()
        {
            CommandSchedulerDbContext.NameOrConnectionString =
                @"Data Source=(localdb)\MSSQLLocalDB; Integrated Security=True; MultipleActiveResultSets=False; Initial Catalog=ItsCqrsTestsCommandScheduler";

            var scenarioBuilder = new ScenarioBuilder()
                .UseSqlCommandScheduler();

            scenarioBuilder.Configuration.UseSqlEventStore();

            return scenarioBuilder;
        }
    }
}