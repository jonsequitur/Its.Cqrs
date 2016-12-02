// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using static Microsoft.Its.Domain.Tests.NonEventSourcedCommandTarget;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class CommandFailedTests
    {
        [Test]
        public void When_a_cancel_has_been_signaled_then_IsCanceled_returns_true()
        {
            var failed = new CommandFailed(new ScheduledCommand<NonEventSourcedCommandTarget>(new TestCommand(), Any.Guid()));

            failed.Cancel();

            failed.IsCanceled
                  .Should()
                  .BeTrue();
        }

        [Test]
        public void When_a_cancel_has_been_not_signaled_then_IsCanceled_returns_false()
        {
            var failed = new CommandFailed(new ScheduledCommand<NonEventSourcedCommandTarget>(new TestCommand(), Any.Guid()));

            failed.IsCanceled
                  .Should()
                  .BeFalse();
        }

        [Test]
        public void NumberOfPreviousAttempts_returns_the_value_set_on_the_scheduled_command()
        {
            var failed = new CommandFailed(new ScheduledCommand<NonEventSourcedCommandTarget>(new TestCommand(), Any.Guid())
            {
                NumberOfPreviousAttempts = 8
            });

            failed.NumberOfPreviousAttempts.Should().Be(8);
        }

        [Test]
        public void When_a_retry_is_not_pending_then_WillBeRetried_returns_false()
        {
            var failed = new CommandFailed(new ScheduledCommand<NonEventSourcedCommandTarget>(new TestCommand(), Any.Guid()));

            failed.WillBeRetried
                  .Should()
                  .BeFalse();
        }

        [Test]
        public void When_a_retry_is_pending_then_WillBeRetried_returns_true()
        {
            var failed = new CommandFailed(new ScheduledCommand<NonEventSourcedCommandTarget>(new TestCommand(), Any.Guid()));

            failed.Retry();

            failed.WillBeRetried
                  .Should()
                  .BeTrue();
        }
    }
}