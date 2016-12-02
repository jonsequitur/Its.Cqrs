// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ScenarioBuilderWithInMemoryEventStoreTests : ScenarioBuilderTests
    {
        protected override ScenarioBuilder CreateScenarioBuilder()
        {
            return new ScenarioBuilder(c => c.UseInMemoryCommandScheduling());
        }

        public override bool UsesSqlStorage => false;

        [Test]
        public void Event_streams_are_shared_among_repository_instances()
        {
            var scenario = CreateScenarioBuilder().Prepare();

            var id = Any.Guid();
            scenario.SaveAsync(new Order(new CreateOrder(id, Any.FullName()))).Wait();

            var order = scenario.GetLatestAsync<Order>(id);

            order.Should().NotBeNull();
        }
    }
}