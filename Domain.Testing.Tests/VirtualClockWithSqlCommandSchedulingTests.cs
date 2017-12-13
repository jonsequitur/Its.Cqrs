// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Sql.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;
using static Microsoft.Its.Domain.Sql.Tests.TestDatabases;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class VirtualClockWithSqlCommandSchedulingTests : VirtualClockCommandSchedulingTests
    {
        private CompositeDisposable disposables;
        private Configuration configuration;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable();

            Command<Order>.AuthorizeDefault = (order, command) => true;
            configuration = GetConfiguration();
            disposables.Add(ConfigurationContext.Establish(configuration));
            disposables.Add(configuration);
        }

        [TearDown]
        public void TearDown()
        {
            Clock.Reset();
            disposables.Dispose();
        }

        protected override Configuration GetConfiguration()
        {
            var clockName = Any.CamelCaseName();
            return new Configuration()
                .UseDependency<GetClockName>(_ => c => clockName)
                .UseSqlStorageForScheduledCommands(c => c.UseConnectionString(TestDatabases.CommandScheduler.ConnectionString))
                .UseSqlEventStore(c => c.UseConnectionString(EventStore.ConnectionString))
                .UseInMemoryCommandTargetStore()
                .UseDefaultSerialization()
                .TraceScheduledCommands();
        }
    }
}