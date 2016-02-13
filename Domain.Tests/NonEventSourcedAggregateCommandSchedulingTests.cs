// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
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
        private InMemoryStore<CommandTarget> store;
        private ICommandScheduler<CommandTarget> scheduler;

        [SetUp]
        public void SetUp()
        {
            disposables = new CompositeDisposable
            {
                VirtualClock.Start()
            };

            targetId = Any.Word();
            target = new CommandTarget(targetId);
            store = new InMemoryStore<CommandTarget>(
                _ => _.Id,
                id => new CommandTarget(id))
            {
                target
            };

            configuration = new Configuration()
                .UseInMemoryCommandScheduling()
                .UseDependency<IStore<CommandTarget>>(_ => store)
                .TraceScheduledCommands();

            scheduler = configuration.CommandScheduler<CommandTarget>();

            Command<CommandTarget>.AuthorizeDefault = (commandTarget, command) => true;

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
                               .Schedule(targetId, new TestCommand());

            target.CommandsEnacted
                  .Should()
                  .ContainSingle(c => c is TestCommand);
        }

        [Test]
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(targetId, new TestCommand(isValid: false));

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
                               .Schedule(targetId,
                                         new TestCommand(isValid: false),
                                         dueTime: Clock.Now().AddMinutes(1));
            await configuration.CommandScheduler<CommandTarget>()
                               .Schedule(targetId,
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
        public async Task Scheduled_commands_triggered_by_a_scheduled_command_are_idempotent()
        {
            var id = Any.Word();
            await store.Put(new CommandTarget(id));

            var command = new SendRequests(new[] { id })
            {
                ETag = "hello".ToETag()
            };

            await scheduler.Schedule(id, command);
            await scheduler.Schedule(id, command);

            var recipient = await store.Get(id);

            recipient
                .CommandsEnacted
                .OfType<SendRequests>()
                .Should()
                .HaveCount(1);
        }

        [Test]
        public async Task Scatter_gather_produces_a_unique_etag_per_sent_command()
        {
            var recipientIds = Enumerable.Range(1, 10)
                                         .Select(_ => Any.Word())
                                         .ToArray();

            await target.ApplyAsync(new SendRequests(recipientIds));

            var receivedCommands = store.SelectMany(t => t.CommandsEnacted);

            receivedCommands 
                .Select(c => c.ETag)
                .Should()
                .OnlyHaveUniqueItems();
        }

        [Ignore]
        [Test]
        public async Task Multiple_scheduled_commands_having_the_some_causative_command_etag_have_repeatable_and_unique_etags()
        {
            var id = Any.Word();
            await store.Put(new CommandTarget(id));

            var command = new SendRequests(new[] { Any.Word() })
            {
                ETag = "one".ToETag()
            };

            var scheduled = new ScheduledCommand<CommandTarget>(
                command, 
                id);

            await scheduler.Deliver(scheduled);

            Assert.Fail("Test not written yet.");
        }
    }
}