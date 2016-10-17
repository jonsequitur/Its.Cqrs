// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Microsoft.Its.Domain.Testing;
#pragma warning disable 618

namespace Microsoft.Its.Domain.Tests
{
    public static class CurrentConfiguration
    {
        public static Task<SchedulerAdvancedResult> AdvanceClock(
            TimeSpan by,
            string clockName = null) =>
                Configuration
                    .Current
                    .SchedulerClockTrigger()
                    .AdvanceClock(
                        clockName: clockName ?? DefaultClockName(),
                        by: by);

        public static Task<SchedulerAdvancedResult> AdvanceClock(
            DateTimeOffset to,
            string clockName = null) =>
                Configuration
                    .Current
                    .SchedulerClockTrigger()
                    .AdvanceClock(
                        clockName: clockName ?? DefaultClockName(),
                        to: to);

        public static string DefaultClockName() =>
            Configuration
                .Current
                .DefaultClockName();

        public static async Task<TTarget> Get<TTarget>(
            string targetId)
            where TTarget : class =>
                await Configuration
                          .Current
                          .Store<TTarget>()
                          .Get(targetId);

        public static async Task<TAggregate> Get<TAggregate>(
            Guid aggregateId)
            where TAggregate : class, IEventSourced =>
                await Configuration
                          .Current
                          .Repository<TAggregate>()
                          .GetLatest(aggregateId);

        public static async Task Save<TTarget>(TTarget target)
            where TTarget : class =>
                await Configuration.Current.Store<TTarget>().Put(target);

        public static Task Deliver<TTarget>(IScheduledCommand<TTarget> scheduledCommand)
            where TTarget : class =>
                Configuration
                    .Current
                    .CommandDeliverer<TTarget>()
                    .Deliver(scheduledCommand);

        public static Task<IScheduledCommand<TAggregate>> Schedule<TAggregate>(
            Guid aggregateId,
            ICommand<TAggregate> command,
            DateTimeOffset? dueTime = null,
            IEvent deliveryDependsOn = null,
            IClock clock = null)
            where TAggregate : class, IEventSourced =>
                Configuration
                    .Current
                    .CommandScheduler<TAggregate>()
                    .Schedule(
                        aggregateId,
                        command,
                        dueTime,
                        deliveryDependsOn,
                        clock);

        public static Task<IScheduledCommand<TTarget>> Schedule<TTarget>(
            string targetId,
            ICommand<TTarget> command,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null,
            IClock clock = null)
            where TTarget : class =>
                Configuration
                    .Current
                    .CommandScheduler<TTarget>()
                    .Schedule(
                        targetId,
                        command,
                        dueTime,
                        deliveryDependsOn,
                        clock);

        public static Task<IScheduledCommand<TTarget>> Schedule<TTarget>(
            ConstructorCommand<TTarget> command,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null,
            IClock clock = null)
            where TTarget : class =>
                Configuration
                    .Current
                    .CommandScheduler<TTarget>()
                    .Schedule(
                        command,
                        dueTime,
                        deliveryDependsOn,
                        clock);

        public static async Task SchedulerWorkComplete() =>
            await Configuration
                      .Current
                      .SchedulerClockTrigger()
                      .Done(DefaultClockName());
    }
}