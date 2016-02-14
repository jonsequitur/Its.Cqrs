// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Pocket;

namespace Microsoft.Its.Domain.Sql
{
    internal class CommandSchedulerResolver
    {
        private readonly Dictionary<string, Func<object>> schedulerResolversByAggregateTypeName;

        public CommandSchedulerResolver(PocketContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            schedulerResolversByAggregateTypeName = new Dictionary<string, Func<dynamic>>();

            Command.KnownTargetTypes.ForEach(aggregateType =>
            {
                var schedulerType = typeof (ICommandScheduler<>).MakeGenericType(aggregateType);

                schedulerResolversByAggregateTypeName.Add(
                    AggregateType.EventStreamName(aggregateType),
                    () => container.Resolve(schedulerType));
            });
        }

        public dynamic ResolveSchedulerForAggregateTypeNamed(string aggregateType)
        {
            var resolver = schedulerResolversByAggregateTypeName[aggregateType];
            return resolver();
        }
    }
}