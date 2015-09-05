// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    internal static class PipelineExtensions
    {
        public static ScheduledCommandPipelineDelegate<TAggregate> Compose<TAggregate>(
            this IEnumerable<ScheduledCommandPipelineDelegate<TAggregate>> pipeline)
            where TAggregate : IEventSourced
        {
            var delegates = pipeline.OrEmpty().ToArray();

            if (!delegates.Any())
            {
                return null;
            }

            return delegates.Aggregate(
                (first, second) =>
                    (async (command, next) =>
                    await first(command,
                                async c => await second(c,
                                                        async cc => await next(cc)))));
        }
    }
}