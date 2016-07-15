// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    internal static class CommandDeliverer
    {
        internal static ICommandDeliverer<TAggregate> Create<TAggregate>(
            Func<IScheduledCommand<TAggregate>, Task> schedule) =>
                new AnonymousCommandDeliverer<TAggregate>(schedule);

        internal static ICommandDeliverer<TAggregate> InterceptDeliver<TAggregate>(
            this ICommandDeliverer<TAggregate> deliverer,
            ScheduledCommandInterceptor<TAggregate> deliver = null)
        {
            deliver = deliver ?? (async (c, next) => await next(c));

            return Create<TAggregate>(
                async command => await deliver(command, async c => await deliverer.Deliver(c)));
        }
    }
}