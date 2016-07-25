// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    [UseInMemoryEventStore]
    [UseInMemoryCommandTargetStore]
    [UseInMemoryCommandScheduling]
    public class InMemoryCommandSchedulerIdempotencyTests_NonEventSourced : CommandSchedulerIdempotencyTests
    {
        protected override Task Schedule(
            string targetId,
            string etag,
            DateTimeOffset? dueTime = null,
            IPrecondition deliveryDependsOn = null) =>
                ScheduleCommandAgainstNonEventSourcedAggregate(targetId,
                    etag,
                    dueTime,
                    deliveryDependsOn);
    }
}