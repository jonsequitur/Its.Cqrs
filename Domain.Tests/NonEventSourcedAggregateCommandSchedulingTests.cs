// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Its.Validation;
using Its.Validation.Configuration;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class NonEventSourcedAggregateCommandSchedulingTests
    {
        private CompositeDisposable disposables = new CompositeDisposable();
        private Configuration configuration;
        private CommandTarget target;
        private string targetId;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable
            {
                VirtualClock.Start()
            };

            targetId = Guid.NewGuid().ToString();
            target = new CommandTarget();

            var store = new InMemoryStore<CommandTarget>(_ => targetId)
            {
                target
            };

            Command<CommandTarget>.AuthorizeDefault = (commandTarget, command) => true;

            configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .UseDependency<IStore<CommandTarget>>(_ => store)
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
            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(Guid.Parse(targetId), new TestCommand());

            target.CommandsEnacted
                  .Should()
                  .ContainSingle(c => c is TestCommand);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(Guid.Parse(targetId), new TestCommand(isValid: false));

            target.CommandsFailed
                  .Select(f => f.ScheduledCommand)
                  .Cast<IScheduledCommand<CommandTarget>>()
                  .Should()
                  .ContainSingle(c => c.Command is TestCommand);
        }

        [Test]
        public async Task When_applying_a_scheduled_command_throws_then_further_command_scheduling_is_not_interrupted()
        {
            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(Guid.Parse(targetId),
                                         new TestCommand(isValid: false),
                                         dueTime: Clock.Now().AddMinutes(1));
            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(Guid.Parse(targetId),
                                         new TestCommand(),
                                         dueTime: Clock.Now().AddMinutes(2));

            VirtualClock.Current.AdvanceBy(TimeSpan.FromHours(1));

            target.CommandsEnacted
                  .Should()
                  .ContainSingle(c => c is TestCommand);
            target.CommandsFailed
                  .Select(f => f.ScheduledCommand)
                  .Cast<IScheduledCommand<CommandTarget>>()
                  .Should()
                  .ContainSingle(c => c.Command is TestCommand);
        }

        [Ignore]
        [Test]
        public void If_Schedule_is_dependent_on_an_event_with_no_aggregate_id_then_it_throws()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public async Task If_Schedule_is_dependent_on_a_precondition_with_no_ETag_then_it_throws()
        {
            var scheduler = configuration
                .UseInMemoryEventStore()
                .UseInMemoryCommandScheduling()
                .CommandScheduler<CommandTarget>();

            var precondition = new CommandPrecondition(etag: "hello".ToETag(), scope: Any.Word());

            await scheduler.Schedule(
                Any.Guid().ToString(),
                new TestCommand(),
                deliveryDependsOn: precondition);

            precondition.ETag.Should().NotBeNullOrEmpty();
        }

        [Ignore]
        [Test]
        public async Task Scheduled_commands_triggered_by_a_scheduled_command_are_idempotent()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public async Task Scatter_gather_produces_a_unique_etag_per_sent_command()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public async Task Multiple_scheduled_commands_having_the_some_causative_command_etag_have_repeatable_and_unique_etags()
        {
            Assert.Fail("Test not written yet.");
        }
    }

    public class CommandTarget
    {
        private readonly ConcurrentBag<ICommand<CommandTarget>> commandsEnacted = new ConcurrentBag<ICommand<CommandTarget>>();
        private readonly ConcurrentBag<CommandFailed> commandsFailed = new ConcurrentBag<CommandFailed>();

        public CommandTarget(CreateCommandTarget create = null)
        {
        }

        public ConcurrentBag<ICommand<CommandTarget>> CommandsEnacted
        {
            get
            {
                return commandsEnacted;
            }
        }

        public ConcurrentBag<CommandFailed> CommandsFailed
        {
            get
            {
                return commandsFailed;
            }
        }
    }

    public class CommandTargetCommandHandler : ICommandHandler<CommandTarget, TestCommand>
    {
        public async Task EnactCommand(CommandTarget target, TestCommand command)
        {
            target.CommandsEnacted.Add(command);
        }

        public async Task HandleScheduledCommandException(CommandTarget target, CommandFailed<TestCommand> command)
        {
            target.CommandsFailed.Add(command);
        }
    }

    public class CreateCommandTarget : ConstructorCommand<CommandTarget>
    {
    }

    public class TestCommand : Command<CommandTarget>
    {
        private readonly bool isValid;

        public TestCommand(string etag = null, bool isValid = true) : base(etag)
        {
            this.isValid = isValid;
        }

        public override IValidationRule CommandValidator
        {
            get
            {
                return Validate.That<TestCommand>(cmd => cmd.isValid);
            }
        }
    }
}