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
            var command = scheduled.ToScheduledCommand<TAggregate>();

            //here we are setting the command.SequenceNumber to the scheduled.SequenceNumber because when
            //multiple commands are scheduled simultaniously against the same aggregate we were decrementing the 
            //scheduled.SequenceNumber correctly, however we were not updating the command.SequenceNumber.
            //this is to prevent any side effects that may have been caused by that bug
            command.SequenceNumber = scheduled.SequenceNumber;

            await scheduler.Deliver(command);

            scheduled.Result = command.Result;
        }

        public IDisposable SubscribeToBus(object handler, IEventBus bus)
        {
            return bus.Subscribe(scheduler);
        }
    }
}