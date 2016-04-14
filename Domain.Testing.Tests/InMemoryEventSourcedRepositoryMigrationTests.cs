// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Domain.Tests;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class InMemoryEventSourcedRepositoryMigrationTests : EventMigrationTests
    {
        protected override IEventSourcedRepository<Order> CreateRepository()
        {
            return new InMemoryEventSourcedRepository<Order>();
        }
    }
}