// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Testing;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public class SqlCommandSchedulerIdempotencyTests_NonEventSourced : SqlCommandSchedulerIdempotencyTests
    {
        protected override void Configure(Configuration configuration, Action<IDisposable> onDispose)
        {
            base.Configure(configuration, onDispose);

            configuration.UseInMemoryCommandTargetStore();
        }

        protected override ScheduleCommand GetScheduleDelegate()
        {
            return ScheduleCommandAgainstNonEventSourcedAggregate;
        }
    }
}