// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Domain.Tests;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public class SqlCommandSchedulerTests_NonEventSourced : SqlCommandSchedulerTests
    {
        private ICommandScheduler<CommandTarget> scheduler;
        private string clockName;
        private CompositeDisposable disposables;
        private EventStoreDbTest eventStoreDbTest;
        private IStore<CommandTarget> store;

        public SqlCommandSchedulerTests_NonEventSourced()
        {
            Command<CommandTarget>.AuthorizeDefault = (target, command) => true;
        }

        [SetUp]
        public void SetUp()
        {
            eventStoreDbTest = new EventStoreDbTest();
            clockName = Any.CamelCaseName();

            Clock.Reset();

            disposables = new CompositeDisposable
            {
                Disposable.Create(() => eventStoreDbTest.TearDown()),
                Disposable.Create(Clock.Reset)
            };

            var configuration = new Configuration();

            Configure(configuration);

            disposables.Add(ConfigurationContext.Establish(configuration));
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        protected override void Configure(Configuration configuration)
        {
            disposables = new CompositeDisposable();
            clockName = Any.CamelCaseName();

            configuration.UseSqlStorageForScheduledCommands()
                         .UseInMemoryCommandTargetStore()
                         .TraceScheduledCommands()
                         .UseDependency<GetClockName>(_ => command => clockName);

            scheduler = configuration.CommandScheduler<CommandTarget>();

            store = configuration.Store<CommandTarget>();
        }

        [Test]
        public override async Task When_a_clock_is_advanced_its_associated_commands_are_triggered()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);

            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     Clock.Now().AddDays(1));

            // act
            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName: clockName,
                                             @by: TimeSpan.FromDays(1.1));

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public override async Task When_a_clock_is_advanced_then_commands_are_not_triggered_that_have_not_become_due()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            var store = Configuration.Current.Store<CommandTarget>();
            await store.Put(target);

            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     Clock.Now().AddDays(2));

            // act
            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName: clockName,
                                             @by: TimeSpan.FromDays(1.1));

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(0);
        }

        [Ignore]
        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_domain_clock()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_scheduler_clock()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task A_command_handler_can_control_retries_of_a_failed_command()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task A_command_handler_can_request_retry_of_a_failed_command_as_soon_as_possible()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task A_command_handler_can_request_retry_of_a_failed_command_as_late_as_it_wants()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task A_command_handler_can_cancel_a_scheduled_command_after_it_fails()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task Specific_scheduled_commands_can_be_triggered_directly_by_target_id()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task When_triggering_specific_commands_then_the_result_can_be_used_to_evaluate_failures()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task When_a_command_is_scheduled_but_an_exception_is_thrown_in_a_handler_then_an_error_is_recorded()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task When_a_command_is_scheduled_but_the_target_it_applies_to_is_not_found_then_the_command_is_retried()
        {
            Assert.Fail("Test not written yet.");
        }

        [Test]
        public override async Task Constructor_commands_can_be_scheduled_to_create_new_aggregate_instances()
        {
            var id = Any.CamelCaseName();
            await scheduler.Schedule(id,
                                     new CreateCommandTarget(id),
                                     Clock.Now().AddDays(30));

            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName: clockName,
                                             @by: TimeSpan.FromDays(31));

            var target = await store.Get(id);

            target.Should().NotBeNull();
        }

        [Ignore]
        [Test]
        public override async Task When_a_constructor_command_fails_with_a_ConcurrencyException_it_is_not_retried()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task When_an_immediately_scheduled_command_depends_on_a_precondition_that_has_not_been_met_yet_then_there_is_not_initially_a_concurrency_exception()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task When_a_scheduled_command_depends_on_an_event_that_never_arrives_it_is_eventually_abandoned()
        {
            Assert.Fail("Test not written yet.");
        }

        [Ignore]
        [Test]
        public override async Task When_command_is_durable_but_immediate_delivery_succeeds_then_it_is_not_redelivered()
        {
            Assert.Fail("Test not written yet.");
        }
    }
}