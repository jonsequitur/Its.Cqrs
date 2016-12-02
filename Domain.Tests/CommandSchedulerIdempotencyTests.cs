// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    [DisableCommandAuthorization]
    public abstract class CommandSchedulerIdempotencyTests
    {
        [Test]
        public async Task When_Schedule_is_called_with_a_command_having_the_same_target_and_etag_then_it_is_not_delivered_a_second_time()
        {
            var targetId = Any.Guid().ToString();
            var etag = Any.Guid().ToString().ToETag();
            var commandsDelivered = new ConcurrentBag<IScheduledCommand>();

            Configuration.Current
                         .TraceScheduledCommands(
                             onDelivered: c => commandsDelivered.Add(c));

            await Schedule(targetId, etag);
            await Schedule(targetId, etag);

            commandsDelivered.Should().HaveCount(1);
        }

        [Test]
        public async Task When_Schedule_is_called_with_a_command_having_the_same_target_and_etag_then_it_is_not_scheduled_a_second_time()
        {
            var targetId = Any.Guid().ToString();
            var etag = Any.Guid().ToString().ToETag();

            var commandsScheduled = new ConcurrentBag<IScheduledCommand>();

            Configuration.Current
                         .TraceScheduledCommands(
                             onScheduled: c => commandsScheduled.Add(c));

            await Schedule(targetId, etag, dueTime: Clock.Now().AddHours(2));
            await Schedule(targetId, etag, dueTime: Clock.Now().AddHours(2));

            commandsScheduled
                .Should()
                .ContainSingle(c => c.Result is CommandScheduled)
                .And
                .ContainSingle(c => c.Result is CommandDeduplicated);
        }

        protected abstract Task Schedule(
            string targetId,
            string etag,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null);

        protected static async Task ScheduleCommandAgainstEventSourcedAggregate(
            string targetId,
            string etag,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null)
        {
            var aggregateId = Guid.Parse(targetId);

            var repository = Configuration.Current.Repository<Order>();

            if (await repository.GetLatest(aggregateId) == null)
            {
                await repository.Save(new Order(new CreateOrder(aggregateId, Any.FullName())));
            }

            var command = new AddItem
            {
                ETag = etag,
                ProductName = Any.Word(),
                Price = 10m
            };

            var scheduledCommand = new ScheduledCommand<Order>(
                command,
                aggregateId,
                dueTime,
                deliveryDependsOn);

            var scheduler = Configuration.Current.CommandScheduler<Order>();

            await scheduler.Schedule(scheduledCommand);
        }

        protected async Task ScheduleCommandAgainstNonEventSourcedAggregate(
            string targetId,
            string etag,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null)
        {
            var repository = Configuration.Current.Store<NonEventSourcedCommandTarget>();

            if (await repository.Get(targetId) == null)
            {
                await repository.Put(new NonEventSourcedCommandTarget(targetId));
            }

            var command = new ScheduledCommand<NonEventSourcedCommandTarget>(
                new NonEventSourcedCommandTarget.TestCommand(etag),
                targetId,
                dueTime,
                deliveryDependsOn);

            var scheduler = Configuration.Current.CommandScheduler<NonEventSourcedCommandTarget>();

            await scheduler.Schedule(command);
        }
    }
}