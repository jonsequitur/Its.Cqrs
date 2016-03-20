// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
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
                Disposable.Create(Clock.Reset),
                VirtualClock.Start()
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

        private async Task AdvanceClock(TimeSpan @by)
        {
            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName: clockName,
                                             @by: @by);
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
            await AdvanceClock(25.Hours());

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public override async Task When_a_clock_is_advanced_then_commands_are_not_triggered_that_have_not_become_due()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     Clock.Now().AddDays(2));

            await AdvanceClock(TimeSpan.FromDays(1.1));

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(0);
        }

        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_domain_clock()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     Clock.Now().AddMinutes(-2));

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public override async Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_scheduler_clock()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);
            var clockRepository = Configuration.Current.SchedulerClockRepository();
            var schedulerClockTime = DateTimeOffset.Parse("2016-02-13 03:03:48 PM");
            clockRepository.CreateClock(clockName, schedulerClockTime);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     schedulerClockTime.AddMinutes(-2));

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public override async Task Immediately_scheduled_commands_triggered_by_a_scheduled_command_have_their_due_time_set_to_the_causative_command_clock()
        {
            // arrange
            var deliveredTime = new DateTimeOffset();
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);
            var clockRepository = Configuration.Current.SchedulerClockRepository();
            var schedulerClockTime = DateTimeOffset.Parse("2016-02-13 01:00:00 AM");
            clockRepository.CreateClock(clockName, schedulerClockTime);
            Configuration.Current.UseCommandHandler<CommandTarget, TestCommand>(async (_, __) =>
            {
                if (__.ETag == "first")
                {
                    await Configuration.Current.CommandScheduler<CommandTarget>().Schedule(target.Id, new TestCommand
                    {
                          CanBeDeliveredDuringScheduling = true
                    });
                }
                else
                {
                    deliveredTime = Clock.Now();
                }
            });

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand
                                     {
                                         CanBeDeliveredDuringScheduling = true,
                                         ETag = "first"
                                     },
                                     dueTime: DateTimeOffset.Parse("2016-02-13 01:05:00 AM"));

            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName, by: 1.Hours());

            // assert 
            deliveredTime.Should().Be(DateTimeOffset.Parse("2016-02-13 01:05:00 AM"));
        }

        [Test]
        public override async Task Scheduled_commands_with_no_due_time_set_the_correct_clock_time_when_delivery_is_deferred()
        {
            // arrange
            var deliveredTime = new DateTimeOffset();
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);
            var clockRepository = Configuration.Current.SchedulerClockRepository();
            var schedulerClockTime = DateTimeOffset.Parse("2016-02-13 01:00:00 AM");
            clockRepository.CreateClock(clockName, schedulerClockTime);
            Configuration.Current.UseCommandHandler<CommandTarget,TestCommand>(async (_,__) => deliveredTime = Clock.Now());

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand
                                     {
                                         CanBeDeliveredDuringScheduling = false
                                     },
                                     dueTime: null);

            await Configuration.Current
                               .SchedulerClockTrigger()
                               .AdvanceClock(clockName, by: 1.Hours());

            // assert 
            deliveredTime.Should().Be(DateTimeOffset.Parse("2016-02-13 01:00:00 AM"));
        }

        [Test]
        public override async Task A_command_handler_can_request_retry_of_a_failed_command_as_soon_as_possible()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName())
            {
                OnHandleScheduledCommandError = (commandTarget, failed) =>
                                                failed.Retry(after: 1.Milliseconds())
            };
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(isValid: false),
                                     Clock.Now().Add(2.Minutes()));
            await AdvanceClock(5.Minutes());
            await AdvanceClock(1.Seconds()); // should trigger a retry

            //assert 
            target = await store.Get(target.Id);

            target.CommandsFailed.Should().HaveCount(2);
        }

        [Test]
        public override async Task A_command_handler_can_request_retry_of_a_failed_command_as_late_as_it_wants()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName())
            {
                OnHandleScheduledCommandError = (commandTarget, failed) =>
                                                failed.Retry(after: 1.Hours())
            };
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(isValid: false),
                                     Clock.Now().Add(2.Minutes()));
            await AdvanceClock(5.Minutes());
            await AdvanceClock(5.Minutes()); // should not trigger a retry

            //assert 
            target = await store.Get(target.Id);

            target.CommandsFailed.Should().HaveCount(1);
        }

        [Test]
        public override async Task A_command_handler_can_cancel_a_scheduled_command_after_it_fails()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName())
            {
                OnHandleScheduledCommandError = (commandTarget, failed) => failed.Cancel()
            };
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(isValid: false),
                                     Clock.Now().AddMinutes(2));

            await AdvanceClock(5.Minutes());

            //assert 
            target = await store.Get(target.Id);

            target.CommandsFailed.Should().HaveCount(1);
        }

        [Test]
        public override async Task Specific_scheduled_commands_can_be_triggered_directly_by_target_id()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(),
                                     Clock.Now().AddDays(2));

            using (var db = new CommandSchedulerDbContext())
            {
                var aggregateId = target.Id.ToGuidV3();
                var command = db.ScheduledCommands
                                .Single(c => c.AggregateId == aggregateId);
                await Configuration.Current
                                   .SchedulerClockTrigger()
                                   .Trigger(command, new SchedulerAdvancedResult(), db);
            }

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public override async Task When_triggering_specific_commands_then_the_result_can_be_used_to_evaluate_failures()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);
            var schedulerAdvancedResult = new SchedulerAdvancedResult();

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(isValid: false),
                                     Clock.Now().AddDays(2));

            using (var db = new CommandSchedulerDbContext())
            {
                var aggregateId = target.Id.ToGuidV3();
                var command = db.ScheduledCommands
                                .Single(c => c.AggregateId == aggregateId);

                await Configuration.Current
                                   .SchedulerClockTrigger()
                                   .Trigger(command, schedulerAdvancedResult, db);
            }

            //assert 
            schedulerAdvancedResult
                .FailedCommands
                .Should()
                .HaveCount(1);
        }

        [Test]
        public override async Task When_a_command_is_scheduled_but_an_exception_is_thrown_in_a_handler_then_an_error_is_recorded()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand(isValid: false));

            //assert 
            using (var db = new CommandSchedulerDbContext())
            {
                var aggregateId = target.Id.ToGuidV3();
                var error = db.Errors.Single(e => e.ScheduledCommand.AggregateId == aggregateId);

                error.Error.Should().Contain("CommandValidationException");
            }
        }

        [Test]
        public override async Task When_a_command_is_scheduled_but_the_target_it_applies_to_is_not_found_then_the_command_is_retried()
        {
            // arrange
            var deliveryAttempts = 0;
            Configuration.Current.AddToCommandSchedulerPipeline<CommandTarget>(
                deliver: async (command, next) =>
                {
                    deliveryAttempts++;
                    await next(command);
                });

            // act
            await scheduler.Schedule(Any.CamelCaseName(),
                                     new TestCommand(isValid: false),
                                     Clock.Now().Add(2.Minutes()));
            await AdvanceClock(1.Days());
            await AdvanceClock(1.Days()); // should trigger a retry

            //assert 
            deliveryAttempts.Should().Be(2);
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

        [Test]
        public override async Task When_a_constructor_command_fails_with_a_ConcurrencyException_it_is_not_retried()
        {
            // arrange
            var deliveredEtags = new List<string>();
            Configuration.Current.AddToCommandSchedulerPipeline<CommandTarget>(
                deliver: async (scheduled, next) =>
                {
                    deliveredEtags.Add(scheduled.Command.ETag);
                    await next(scheduled);
                });

            var id = Any.CamelCaseName();
            var etag1 = Any.Word().ToETag();
            var etag2 = Any.Word().ToETag();
            var command1 = new CreateCommandTarget(id, etag: etag1);
            var command2 = new CreateCommandTarget(id, etag: etag2);

            // act
            await scheduler.Schedule(id, command1);
            await scheduler.Schedule(id, command2);

            await AdvanceClock(1.Days());
            await AdvanceClock(1.Days());
            await AdvanceClock(1.Days());

            // assert
            deliveredEtags.Should()
                          .ContainSingle(e => e == etag1)
                          .And
                          .ContainSingle(e => e == etag2);
        }

        [Test]
        public override async Task When_an_immediately_scheduled_command_depends_on_a_precondition_that_has_not_been_met_yet_then_there_is_not_initially_an_attempt_recorded()
        {
            // arrange
            var targetId = Any.CamelCaseName();
            var precondition = new EventHasBeenRecordedPrecondition(
                Guid.NewGuid().ToString().ToETag(),
                Guid.NewGuid());

            // act
            await scheduler.Schedule(targetId,
                                     new CreateCommandTarget(targetId),
                                     deliveryDependsOn: precondition);
        
            // assert
            using (var db = new CommandSchedulerDbContext())
            {
                var aggregateId = targetId.ToGuidV3();
                var command = db.ScheduledCommands.Single(c => c.AggregateId == aggregateId);

                command.AppliedTime
                       .Should()
                       .NotHaveValue();

                command.Attempts
                       .Should()
                       .Be(0);
            }
        }

        [Test]
        public override async Task When_a_scheduled_command_depends_on_an_event_that_never_arrives_it_is_eventually_abandoned()
        {
            // arrange
            var deliveryAttempts = 0;
            Configuration.Current.AddToCommandSchedulerPipeline<CommandTarget>(
                deliver: async (command, next) =>
                {
                    deliveryAttempts++;
                    await next(command);
                });
         
            var precondition = new EventHasBeenRecordedPrecondition(
                Guid.NewGuid().ToString().ToETag(),
                Guid.NewGuid());

            // act
            await scheduler.Schedule(Any.CamelCaseName(),
                                     new TestCommand(),
                                     deliveryDependsOn: precondition);
         
            for (var i = 0; i < 10; i++)
            {
                  await AdvanceClock(1.Days());
            }

            //assert 
            deliveryAttempts.Should().Be(6);
        }

        [Test]
        public override async Task When_command_is_durable_but_immediate_delivery_succeeds_then_it_is_not_redelivered()
        {
            // arrange
            var target = new CommandTarget(Any.CamelCaseName());
            await store.Put(target);

            // act
            await scheduler.Schedule(target.Id,
                                     new TestCommand
                                     {
                                         RequiresDurableScheduling = true
                                     });
            await AdvanceClock(2.Days());

            //assert 
            target = await store.Get(target.Id);

            target.CommandsEnacted.Should().HaveCount(1);
        }

        [Test]
        public override async Task When_a_clock_is_advanced_and_a_command_fails_to_be_deserialized_then_other_commands_are_still_applied()
        {
              var commandScheduler = Configuration.Current.CommandScheduler<CommandTarget>();
        
            var failedTargetId = Any.CamelCaseName();
            var successfulTargetId = Any.CamelCaseName();

            await commandScheduler.Schedule(failedTargetId,
                                            new CreateCommandTarget(failedTargetId),
                                            Clock.Now().AddHours(1)); 
            await commandScheduler.Schedule(successfulTargetId,
                                            new CreateCommandTarget(successfulTargetId),
                                            Clock.Now().AddHours(1.5));

            using (var db = new CommandSchedulerDbContext())
            {
                var aggregateId = failedTargetId.ToGuidV3();

                var command = db.ScheduledCommands.Single(c => c.AggregateId == aggregateId);
                var commandBody = command.SerializedCommand.FromJsonTo<dynamic>();
                commandBody.Command.CommandName = "not a command name";
                command.SerializedCommand = commandBody.ToString();
                db.SaveChanges();
            }

            // act
            Action advanceClock = () => Configuration.Current
                                                     .SchedulerClockTrigger()
                                                     .AdvanceClock(clockName: clockName,
                                                                   @by: TimeSpan.FromHours(2)).Wait();

            // assert
            advanceClock.ShouldNotThrow();

            var successfulAggregate = await store.Get(successfulTargetId);
            successfulAggregate.Should().NotBeNull();
        }

        [Test]
        public override async Task When_a_clock_is_set_on_a_command_then_it_takes_precedence_over_GetClockName()
        {
            // arrange
            var clockName = Any.CamelCaseName();
            var targetId = Any.CamelCaseName();
            var command = new CreateCommandTarget(targetId);
            var scheduledCommand = new ScheduledCommand<CommandTarget>(
                targetId: targetId,
                command: command, 
                dueTime: DateTimeOffset.Parse("2016-03-20 09:00:00 AM"))
            {
                Clock = new CommandScheduler.Clock
                {
                    Name = clockName,
                    UtcNow = DateTimeOffset.Parse("2016-03-01 02:00:00 AM")
                }
            };

            // act
            await scheduler.Schedule(scheduledCommand);

            await Configuration.Current.SchedulerClockTrigger()
                .AdvanceClock(clockName, by: 30.Days());

            //assert 
            var target = await store.Get(targetId);

            target.Should().NotBeNull();
        }
    }
}