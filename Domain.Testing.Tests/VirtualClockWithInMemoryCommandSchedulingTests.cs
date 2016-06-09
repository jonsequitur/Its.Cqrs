// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Microsoft.Its.Domain.Sql.Tests;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class VirtualClockWithInMemoryCommandSchedulingTests : VirtualClockCommandSchedulingTests
    {
        private CompositeDisposable disposables;
        private Configuration configuration;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable();

            Command<Order>.AuthorizeDefault = (order, command) => true;

            TestDatabases.SetConnectionStrings();

            configuration = GetConfiguration();
            disposables.Add(ConfigurationContext.Establish(configuration));
        }

        [TearDown]
        public void TearDown()
        {
            Clock.Reset();
            disposables.Dispose();
        }

        protected override Configuration GetConfiguration()
        {
            return new Configuration()
                .UseInMemoryCommandScheduling()
                .UseInMemoryEventStore()
                .UseInMemoryCommandTargetStore()
                .TraceScheduledCommands();
        }
    }
}