using System;
using FluentAssertions;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class NonAggregateCommandTests
    {
        [Test]
        public void non_event_sourced_command_schedulers_can_be_resolved()
        {
            Action getScheduler = () =>  Configuration.Current.CommandScheduler<Foo>();

            getScheduler.ShouldNotThrow();
        }
    }

    public class Foo
    {
    }
}