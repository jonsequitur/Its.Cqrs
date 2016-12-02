// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public abstract class ScheduledCommandTests
    {
        [Test]
        public void A_ScheduledCommand_is_due_if_no_due_time_is_specified()
        {
            var command = CreateScheduledCommand(new AddItem(), Any.Guid());

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_current_domain_clock()
        {
            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid(), Clock.Now().Subtract(TimeSpan.FromSeconds(1)));

            command.IsDue()
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_due_if_a_due_time_is_specified_that_is_earlier_than_the_specified_clock()
        {
            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid(),
                Clock.Now().Add(TimeSpan.FromDays(1)));

            command.IsDue(Clock.Create(() => Clock.Now().Add(TimeSpan.FromDays(2))))
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_a_due_time_is_specified_that_is_later_than_the_current_domain_clock()
        {
            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid(),
                Clock.Now().Add(TimeSpan.FromSeconds(1)));

            command.IsDue()
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_it_has_already_been_delivered_and_failed()
        {
            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid());

            command.Result = new CommandFailed(command);

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public void A_ScheduledCommand_is_not_due_if_it_has_already_been_delivered_and_succeeded()
        {
            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid());

            command.Result = new CommandSucceeded(command);

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public void When_a_scheduled_command_is_created_with_a_command_having_no_etag_then_a_command_etag_is_set()
        {
            var command = new AddItem
            {
                ETag = null
            };

            var scheduledCommand = CreateScheduledCommand(command, Any.Guid());

            scheduledCommand.Command.ETag.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public void A_ScheduledCommand_cannot_be_set_to_Scheduled_once_it_has_been_Delivered()
        {
            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid());

            command.Result = new CommandSucceeded(command);

            Action setToScheduled = () =>
                                    command.Result = new CommandScheduled(command);

            setToScheduled.ShouldThrow<ArgumentException>()
                          .WithMessage("Command cannot be scheduled again when it has already been delivered.");
        }

        [Test]
        public void A_ScheduledCommand_with_an_event_sourced_target_has_a_non_null_AggregateId()
        {
            var id = Any.Guid();

            var command = CreateScheduledCommand(
                new AddItem(),
                id);

            Guid aggregateId = ((dynamic) command).AggregateId;

            aggregateId.Should().Be(id);
        }

        [Test]
        public void A_scheduled_command_is_due_if_its_due_time_is_equal_to_its_clock_time()
        {
            var dueTime = DateTimeOffset.Parse("2016-03-19 10:22:52 AM");

            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid(),
                dueTime: dueTime,
                clock: Clock.Create(() => dueTime));

            command.IsDue().Should().BeTrue();
        }

        [Test]
        public void A_scheduled_command_is_not_due_if_its_due_time_is_later_then_its_clock_time()
        {
            var dueTime = DateTimeOffset.Parse("2016-03-19 10:22:52 AM");
            var clockTime = DateTimeOffset.Parse("2016-02-19 10:22:52 AM");

            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid(),
                dueTime: dueTime,
                clock: Clock.Create(() => clockTime));

            command.IsDue().Should().BeFalse();
        }

        [Test]
        public void A_scheduled_command_is_due_if_its_due_time_is_earlier_than_its_clock_time()
        {
            var dueTime = DateTimeOffset.Parse("2016-03-19 10:22:52 AM");
            var clockTime = DateTimeOffset.Parse("2016-03-19 11:22:52 AM");

            var command = CreateScheduledCommand(
                new AddItem(),
                Any.Guid(),
                dueTime: dueTime,
                clock: Clock.Create(() => clockTime));

            command.IsDue().Should().BeTrue();
        }

        protected abstract IScheduledCommand<T> CreateScheduledCommand<T>(
            ICommand<T> command,
            Guid aggregateId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null,
            IClock clock = null)
            where T : class, IEventSourced;
    }
}