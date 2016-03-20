// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            var command = new ScheduledCommand<CommandTarget>(new TestCommand(),
                                                              Any.Guid().ToString());

            command.AggregateId.Should().Be(null);
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