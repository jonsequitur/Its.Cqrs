// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NCrunch.Framework;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerIdempotencyTests_EventSourced : SqlCommandSchedulerIdempotencyTests
    {
        protected override ScheduleCommand GetScheduleDelegate()
        {
            return ScheduleCommandAgainstEventSourcedAggregate;
        }
    }
}