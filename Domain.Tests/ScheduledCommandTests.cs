// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class ScheduledCommandTests
    {
        [Test]
        public void A_ScheduledCommand_is_due_if_no_due_time_is_specified()
        {
            var command = new ScheduledCommand<Order>
            {
                Command = new AddItem()
            };

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_current_domain_clock()
        {
            var command = new ScheduledCommand<Order>
            {
                Command = new AddItem(),
                DueTime = Clock.Now().Subtract(TimeSpan.FromSeconds(1))
            };

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_specified_clock()
        {
            var command = new ScheduledCommand<Order>
            {
                Command = new AddItem(),
                DueTime = Clock.Now().Add(TimeSpan.FromDays(1))
            };

            command.IsDue(Clock.Create(() => Clock.Now().Add(TimeSpan.FromDays(2))))
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_a_due_time_is_specified_that_is_later_than_the_current_domain_clock()
        {
            var command = new ScheduledCommand<Order>
            {
                Command = new AddItem(),
                DueTime = Clock.Now().Add(TimeSpan.FromSeconds(1))
            };

            command.IsDue()
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_it_has_already_been_delivered_and_failed()
        {
            var command = new ScheduledCommand<Order>
            {
                Command = new AddItem()
            };
            command.Result = new CommandFailed(command);

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_it_has_already_been_delivered_and_succeeded()
        {
            var command = new ScheduledCommand<Order>
            {
                Command = new AddItem()
            };
            command.Result = new CommandSucceeded(command);

            command.IsDue().Should().BeFalse();
        }
    }
}