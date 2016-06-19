// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Testing;
using NCrunch.Framework;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    [ExclusivelyUses("ItsCqrsTestsEventStore", "ItsCqrsTestsReadModels", "ItsCqrsTestsCommandScheduler")]
    public class SqlCommandSchedulerIdempotencyTests_NonEventSourced : SqlCommandSchedulerIdempotencyTests
    {
        protected override void Configure(Configuration configuration)
        {
            base.Configure(configuration);

            configuration.UseInMemoryCommandTargetStore();
        }

        protected override ScheduleCommand GetScheduleDelegate()
        {
            return ScheduleCommandAgainstNonEventSourcedAggregate;
        }
    }
}