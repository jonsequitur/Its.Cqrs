// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Its.Domain
{
    internal abstract class SchedulerPipelineInitializer : ISchedulerPipelineInitializer
    {
        private readonly MethodInfo initializeFor;

        protected SchedulerPipelineInitializer()
        {
            initializeFor = GetType().GetMethod("InitializeFor", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Initialize(Configuration configuration)
        {
            AggregateType.KnownTypes.ForEach(aggregateType =>
            {
                initializeFor.MakeGenericMethod(aggregateType).Invoke(this, new[] { configuration });
            });
        }

        protected abstract void InitializeFor<TAggregate>(Configuration configuration)
            where TAggregate : class, IEventSourced;
    }
}