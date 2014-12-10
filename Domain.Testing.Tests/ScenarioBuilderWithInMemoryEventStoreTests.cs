using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ScenarioBuilderWithInMemoryEventStoreTests : ScenarioBuilderTests
    {
        protected override ScenarioBuilder CreateScenarioBuilder()
        {
            return new ScenarioBuilder(c => c.UseInMemoryCommandScheduling());
        }

        [Test]
        public void Event_streams_are_shared_among_repository_instances()
        {
            var scenario = CreateScenarioBuilder().Prepare();

            var id = Any.Guid();
            scenario.Save(new Order(new CreateOrder(Any.FullName())
            {
                AggregateId = id
            }));

            var order = scenario.GetLatest<Order>(id);

            order.Should().NotBeNull();
        }
    }
}