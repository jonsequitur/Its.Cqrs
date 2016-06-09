// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.Tests;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ScenarioBuilderWithPipelinedSqlStorageTests : ScenarioBuilderTests
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

        public override bool UsesSqlStorage => true;

        protected override ScenarioBuilder CreateScenarioBuilder()
        {
            TestDatabases.SetConnectionStrings();

            var scenarioBuilder = new ScenarioBuilder();

            scenarioBuilder.Configuration
                           .UseSqlEventStore()
                           .UseSqlStorageForScheduledCommands();

            return scenarioBuilder;
        }
    }
}