// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Sql.CommandScheduler
{
    internal class SqlCommandSchedulerBinder<TAggregate> :
        ICommandSchedulerDispatcher
        where TAggregate : class, IEventSourced
    {
        private readonly SqlCommandScheduler<TAggregate> scheduler;

        public SqlCommandSchedulerBinder(SqlCommandScheduler<TAggregate> scheduler)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            this.scheduler = scheduler;
        }

        public Type EventType
        {
            get
            {
                return typeof (IScheduledCommand<TAggregate>);
            }
        }

        public SqlCommandScheduler<TAggregate> Scheduler
        {
            get
            {
                return scheduler;
            }
        }

        public string AggregateType
        {
            get
            {
                return AggregateType<TAggregate>.EventStreamName;
            }
        }

        public async Task Deliver(ScheduledCommand scheduled)
        {
            await Storage.DeserializeAndDeliverScheduledCommand<TAggregate>(
                scheduled,
                scheduler);
        }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            return bus.Subscribe(scheduler);
        }
    }
}