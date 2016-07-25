// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Tests;
using NCrunch.Framework;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [UseInMemoryCommandTargetStore]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerIdempotencyTests_NonEventSourced : SqlCommandSchedulerIdempotencyTests
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