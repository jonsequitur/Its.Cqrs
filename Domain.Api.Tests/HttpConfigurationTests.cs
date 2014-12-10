using System.Linq;
using System.Web.Http;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Api.Tests
{
    [TestFixture]
    public class HttpConfigurationTests
    {
        [Test]
        public void StartEventHandlers_will_not_start_multiple_instances_of_the_same_handler()
        {
            var configuration = new HttpConfiguration()
                .StartEventHandlers(@where: t => t.Assembly == GetType().Assembly);

            var countBefore = configuration.RunningEventHandlers().Count();

            configuration.StartEventHandlers(@where: t => t.Assembly == GetType().Assembly);

            var countAfter = configuration.RunningEventHandlers().Count();

            countAfter.Should().Be(countBefore);
        }

        [Test]
        public void Handlers_that_handle_multiple_event_types_are_only_instantiated_once()
        {
            var container = new PocketContainer()
                .Register<IEventBus>(c => new InProcessEventBus());

            var configuration = new HttpConfiguration()
                .ResolveDependenciesUsing(container);

            using (configuration.StartEventHandlers(@where: t => t.Assembly == GetType().Assembly))
            {
                var handlers = configuration.RunningEventHandlers();
                handlers.Count().Should().Be(handlers.Distinct().Count());
            }
        }
    }
}