// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Tests
{
    [Category("Command scheduling")]
    [TestFixture]
    public abstract class CommandSchedulerIdempotencyTests
    {
        public delegate Task ScheduleCommand(string targetId,
                                             string etag,
                                             DateTimeOffset? dueTime = null,
                                             IPrecondition deliveryDependsOn = null);

        private CompositeDisposable disposables = new CompositeDisposable();
        private ConcurrentBag<IScheduledCommand> commandsDelivered;
        private ConcurrentBag<IScheduledCommand> commandsScheduled;
        private ScheduleCommand schedule;

        protected abstract void Configure(Configuration configuration, Action<IDisposable> onDispose);

        [SetUp]
        public void SetUp()
        {
            Clock.Reset();

            disposables = new CompositeDisposable
            {
                Disposable.Create(Clock.Reset)
            };

            schedule = GetScheduleDelegate();

            commandsScheduled = new ConcurrentBag<IScheduledCommand>();
            commandsDelivered = new ConcurrentBag<IScheduledCommand>();

            var configuration = new Configuration()
                .TraceScheduledCommands() // trace to console
                .TraceScheduledCommands(
                    onScheduling: _ => { },
                    onScheduled: c => commandsScheduled.Add(c),
                    onDelivering: _ => { },
                    onDelivered: c => commandsDelivered.Add(c));

            Configure(configuration, d => disposables.Add(d));

            disposables.Add(ConfigurationContext.Establish(configuration));
        }

        [TearDown]
        public void TearDown()
        {
            disposables.Dispose();
        }

        [Test]
        public async Task When_Schedule_is_called_with_a_command_having_the_same_target_and_etag_then_it_is_not_delivered_a_second_time()
        {
            var targetId = Any.Guid().ToString();
            var etag = Any.Guid().ToString().ToETag();

            await schedule(targetId, etag);
            await schedule(targetId, etag);

            commandsDelivered.Should().HaveCount(1);
        }

        [Test]
        public async Task When_Schedule_is_called_with_a_command_having_the_same_target_and_etag_then_it_is_not_scheduled_a_second_time()
        {
            var targetId = Any.Guid().ToString();
            var etag = Any.Guid().ToString().ToETag();

            await schedule(targetId, etag, dueTime: Clock.Now().AddHours(2));
            await schedule(targetId, etag, dueTime: Clock.Now().AddHours(2));

            commandsScheduled
                .Should()
                .ContainSingle(c => c.Result is CommandScheduled)
                .And
                .ContainSingle(c => c.Result is CommandDeduplicated);
        }

        protected abstract ScheduleCommand GetScheduleDelegate();

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
                await repository.Save(new Order(new CreateOrder(Any.FullName())
                {
                    AggregateId = aggregateId
                }));
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
            var repository = Configuration.Current.Store<CommandTarget>();

            if (await repository.Get(targetId) == null)
            {
                await repository.Put(new CommandTarget(targetId));
            }

            var command = new ScheduledCommand<CommandTarget>(
                new TestCommand(etag),
                targetId,
                dueTime,
                deliveryDependsOn);

            var scheduler = Configuration.Current.CommandScheduler<CommandTarget>();

            await scheduler.Schedule(command);
        }
    }
}