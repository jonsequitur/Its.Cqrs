// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [UseInMemoryEventStore]
    [UseInMemoryCommandScheduling]
    public class InMemoryCommandSchedulerIdempotencyTests_EventSourced : CommandSchedulerIdempotencyTests
    {
        protected override Task Schedule(
            string targetId,
            string etag,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null) =>
                ScheduleCommandAgainstEventSourcedAggregate(targetId,
                    etag,
                    dueTime,
                    deliveryDependsOn);
    }
}