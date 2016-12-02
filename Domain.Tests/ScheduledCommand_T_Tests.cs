// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    public class ScheduledCommand_T_Tests : ScheduledCommandTests
    {
        [Test]
        public void A_ScheduledCommand_with_an_non_event_sourced_target_has_a_null_AggregateId()
        {
            var command = new ScheduledCommand<NonEventSourcedCommandTarget>(new NonEventSourcedCommandTarget.TestCommand(),
                Any.Guid().ToString());

            command.AggregateId.Should().Be(null);
        }

        [Test]
        public async Task A_scheduled_constructor_command_for_a_non_event_sourced_aggregate_must_have_the_same_aggregate_id_as_the_scheduled_command()
        {
            Action create = () => new ScheduledCommand<NonEventSourcedCommandTarget>(
                new NonEventSourcedCommandTarget.CreateCommandTarget("the-constructor-command-id"),
                "the-scheduled-command-id");

            create.ShouldThrow<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("ConstructorCommand.TargetId (the-constructor-command-id) does not match ScheduledCommand.TargetId (the-scheduled-command-id)");
        }

        [Test]
        public async Task A_scheduled_constructor_command_for_an_event_sourced_aggregate_must_have_the_same_aggregate_id_as_the_scheduled_command()
        {
            var theConstructorCommandId = Any.Guid();
            var theScheduledCommandId = Any.Guid();

            Action create = () => new ScheduledCommand<EventSourcedCommandTarget>(
                new EventSourcedCommandTarget.CreateCommandTarget(theConstructorCommandId),
                theScheduledCommandId);

            create.ShouldThrow<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be($"ConstructorCommand.AggregateId ({theConstructorCommandId}) does not match ScheduledCommand.AggregateId ({theScheduledCommandId})");
        }

        protected override IScheduledCommand<T> CreateScheduledCommand<T>(
            ICommand<T> command,
            Guid aggregateId,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null,
            IClock clock = null)
        {
            return new ScheduledCommand<T>(command,
                aggregateId,
                dueTime,
                deliveryDependsOn)
            {
                Clock = clock
            };
        }
    }
}