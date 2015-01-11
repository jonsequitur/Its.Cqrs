// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class CommandSchedulerExtensions
    {
        public static Task Schedule<TCommand, TAggregate>(
            this ICommandScheduler<TAggregate> scheduler,
            Guid aggregateId,
            TCommand command,
            DateTimeOffset? dueTime = null,
            IEvent deliveryDependsOn = null)
            where TCommand : ICommand<TAggregate>
            where TAggregate : IEventSourced
        {
            if (aggregateId == Guid.Empty)
            {
                throw new ArgumentException("Parameter aggregateId cannot be an empty Guid.");
            }

            ScheduledCommandPrecondition precondition = null;

            if (deliveryDependsOn != null)
            {
                if (deliveryDependsOn.AggregateId == Guid.Empty)
                {
                    throw new ArgumentException("An AggregateId must be set on the event on which the scheduled command depends.");
                }

                if (string.IsNullOrWhiteSpace(deliveryDependsOn.ETag))
                {
                    deliveryDependsOn.IfTypeIs<Event>()
                             .ThenDo(e => e.ETag = Guid.NewGuid().ToString("N"))
                             .ElseDo(() => { throw new ArgumentException("An ETag must be set on the event on which the scheduled command depends."); });
                }

                precondition = new ScheduledCommandPrecondition
                {
                    AggregateId = deliveryDependsOn.AggregateId,
                    ETag = deliveryDependsOn.ETag
                };
            }

            return scheduler.Schedule(new CommandScheduled<TAggregate>
            {
                Command = command,
                DueTime = dueTime,
                AggregateId = aggregateId,
                SequenceNumber = - DateTimeOffset.UtcNow.Ticks,
                DeliveryPrecondition = precondition
            });
        }
    }
}
