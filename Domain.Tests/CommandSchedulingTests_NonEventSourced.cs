// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Testing;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    [UseInMemoryCommandScheduling]
    [UseInMemoryCommandTargetStore]
    [DisableCommandAuthorization]
    public class CommandSchedulingTests_NonEventSourced
    {
        [Test]
        public async Task CommandScheduler_executes_scheduled_commands_immediately_if_no_due_time_is_specified()
        {
            var target = CreateCommandTarget();

            await Configuration.Current
                               .CommandScheduler<NonEventSourcedCommandTarget>()
                               .Schedule(target.Id, new TestCommand());

            target.CommandsEnacted
                  .Should()
                  .ContainSingle(c => c is TestCommand);
        }

        [Test]
        public async Task Multiple_scheduled_commands_having_the_some_causative_command_etag_have_repeatable_and_unique_etags()
        {
            var senderId = Any.Word();
            await Configuration.Current.Store<NonEventSourcedCommandTarget>().Put(new NonEventSourcedCommandTarget(senderId));

            var targetIds = new[] { Any.Word(), Any.Word(), Any.Word() };

            var results = new ConcurrentBag<RequestReply>();

            Configuration.Current
                         .TraceScheduledCommands(
                             onScheduling: cmd =>
                             {
                                 var requestReply = ((dynamic) cmd).Command as RequestReply;
                                 if (requestReply != null)
                                 {
                                     results.Add(requestReply);
                                 }
                             });

            var initialEtag = "initial".ToETag();

            var firstCommand = new SendRequests(targetIds)
                               {
                                   ETag = initialEtag
                               };

            var scheduledCommand = new ScheduledCommand<NonEventSourcedCommandTarget>(
                firstCommand,
                senderId);

            await Configuration.Current
                               .CommandDeliverer<NonEventSourcedCommandTarget>().Deliver(scheduledCommand);

            var secondCommand = new SendRequests(targetIds)
                                {
                                    ETag = initialEtag
                                };

            scheduledCommand = new ScheduledCommand<NonEventSourcedCommandTarget>(
                secondCommand,
                senderId);

            // redeliver
            await Configuration.Current
                               .CommandDeliverer<NonEventSourcedCommandTarget>().Deliver(scheduledCommand);

            Console.WriteLine(results.ToJson());

            results.Should().HaveCount(6);
            results.Select(r => r.ETag)
                   .Distinct()
                   .Should()
                   .HaveCount(3);
        }

        [Test]
        public async Task Scatter_gather_produces_a_unique_etag_per_sent_command()
        {
            var recipientIds = Enumerable.Range(1, 10)
                                         .Select(_ => Any.Word())
                                         .ToArray();

            await CreateCommandTarget().ApplyAsync(new SendRequests(recipientIds));

            var store = Configuration.Current.Store<NonEventSourcedCommandTarget>() as InMemoryStore<NonEventSourcedCommandTarget>;
            var receivedCommands = store.SelectMany(t => t.CommandsEnacted);

            receivedCommands
                .Select(c => c.ETag)
                .Should()
                .OnlyHaveUniqueItems();
        }

        [Test]
        public async Task Scheduled_commands_triggered_by_a_scheduled_command_are_idempotent()
        {
            var id = Any.Word();
            var store = Configuration.Current.Store<NonEventSourcedCommandTarget>();
            await store.Put(new NonEventSourcedCommandTarget(id));

            var command = new SendRequests(new[] { id })
                          {
                              ETag = "hello".ToETag()
                          };

            var scheduler = Configuration.Current.CommandScheduler<NonEventSourcedCommandTarget>();
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
        public async Task When_a_scheduled_command_fails_validation_then_a_failure_event_can_be_recorded_in_HandleScheduledCommandException_method()
        {
            var target = CreateCommandTarget();

            await Configuration.Current
                               .CommandScheduler<NonEventSourcedCommandTarget>()
                               .Schedule(target.Id, new TestCommand(isValid: false));

            target.CommandsFailed
                  .Select(f => f.ScheduledCommand)
                  .Cast<IScheduledCommand<NonEventSourcedCommandTarget>>()
                  .Should()
                  .ContainSingle(c => c.Command is TestCommand);
        }

        [Test]
        public async Task When_applying_a_scheduled_command_throws_then_further_command_scheduling_is_not_interrupted()
        {
            var target = CreateCommandTarget();
            var configuration = Configuration.Current;
            await configuration.CommandScheduler<NonEventSourcedCommandTarget>()
                               .Schedule(target.Id,
                                   new TestCommand(isValid: false),
                                   dueTime: Clock.Now().AddMinutes(1));
            await configuration.CommandScheduler<NonEventSourcedCommandTarget>()
                               .Schedule(target.Id,
                                   new TestCommand(),
                                   dueTime: Clock.Now().AddMinutes(2));

            VirtualClock.Current.AdvanceBy(TimeSpan.FromHours(1));

            target.CommandsEnacted
                  .Should()
                  .ContainSingle(c => c is TestCommand);
            target.CommandsFailed
                  .Select(f => f.ScheduledCommand)
                  .Cast<IScheduledCommand<NonEventSourcedCommandTarget>>()
                  .Should()
                  .ContainSingle(c => c.Command is TestCommand);
        }

        private static NonEventSourcedCommandTarget CreateCommandTarget()
        {
            var target = new NonEventSourcedCommandTarget(Any.Word());
            Configuration.Current.Store<NonEventSourcedCommandTarget>().Put(target);
            return target;
        }
    }
}