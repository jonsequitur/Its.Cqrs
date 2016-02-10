// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Testing;

namespace Microsoft.Its.Domain.Tests
{
    public class InMemoryCommandSchedulerIdempotencyTests_NonEventSourced : CommandSchedulerIdempotencyTests
    {
        protected override void Configure(
            Configuration configuration,
            Action<IDisposable> onDispose)
        {
            Command<CommandTarget>.AuthorizeDefault = (account, command) => true;

            configuration.UseInMemoryCommandTargetStore()
                         .UseInMemoryEventStore()
                         .UseInMemoryCommandScheduling();
        }

        protected override ScheduleCommand GetScheduleDelegate()
        {
            return ScheduleCommandAgainstNonEventSourcedAggregate;
        }
    }
}