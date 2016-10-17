// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain
{
    /// <summary>
    /// A delegate used for composing command scheduler pipelines.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="next">The next stage in the pipeline, which should be called unless the operation is being stopped at the current stage.</param>
    public delegate Task ScheduledCommandInterceptor<TAggregate>(
        IScheduledCommand<TAggregate> command,
        Func<IScheduledCommand<TAggregate>, Task> next);
}