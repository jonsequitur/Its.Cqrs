// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    public delegate Task ScheduledCommandInterceptor<TAggregate>(
        IScheduledCommand<TAggregate> command,
        Func<IScheduledCommand<TAggregate>, Task> next);
}