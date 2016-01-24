// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Testing;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [Ignore("Scenario under development")]
    [Category("Command scheduling")]
    [TestFixture]
    public class NonEventSourcedAggregateCommandSchedulingTests
    {
        private CompositeDisposable disposables;
        private Configuration configuration;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable
            {
                VirtualClock.Start()
            };

            configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .TraceScheduledCommands();

            disposables.Add(ConfigurationContext.Establish(configuration));
            disposables.Add(configuration);
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public async Task CommandScheduler_executes_scheduled_commands_immediately_if_no_due_time_is_specified()
        {
            var target = new CommandTarget();

            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(Guid.NewGuid(), new ValidCommand());

            target.CommandsApplied
                  .Should()
                  .ContainSingle(c => c is ValidCommand);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public async Task When_applying_a_scheduled_command_throws_unexpectedly_then_further_command_scheduling_is_not_interrupted()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public void If_Schedule_is_dependent_on_an_event_with_no_aggregate_id_then_it_throws()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public async Task If_Schedule_is_dependent_on_an_event_with_no_ETag_then_it_sets_one()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public async Task Scheduled_commands_triggered_by_a_scheduled_command_are_idempotent()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public async Task Scatter_gather_produces_a_unique_etag_per_sent_command()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public async Task Multiple_scheduled_commands_having_the_some_causative_command_etag_have_repeatable_and_unique_etags()
        {
            Assert.Fail("Test not written yet.");
        }
    }

    public class CommandTarget
    {
        private readonly ConcurrentBag<ICommand> commandsApplied = new ConcurrentBag<ICommand>();

        public ConcurrentBag<ICommand> CommandsApplied
        {
            get
            {
                return commandsApplied;
            }
        }
    }

    public class CommandTargetCommandHandler : ICommandHandler<CommandTarget, ValidCommand>
    {
        public async Task EnactCommand(CommandTarget target, ValidCommand command)
        {
            target.CommandsApplied.Add(command);
        }

        public async Task HandleScheduledCommandException(CommandTarget target, CommandFailed<ValidCommand> command)
        {
        }
    }

    public class ValidCommand : Command<CommandTarget>
    {
    }
}