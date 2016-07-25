// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Its.Domain.Tests;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [UseSqlStorageForScheduledCommands]
    [UseSqlEventStore]
    public abstract class SqlCommandSchedulerIdempotencyTests : CommandSchedulerIdempotencyTests
    {
    }
}