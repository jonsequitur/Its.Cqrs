// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Sample.Domain.Ordering;
using Sample.Domain.Ordering.Commands;

namespace Microsoft.Its.Domain.Tests
{
    [TestFixture]
    public class SnapshotRepositoryExtensionsTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [Test]
        public async Task SaveSnapshot_throws_an_informative_exception_when_no_snapshot_creator_is_found()
        {
            var repository = new InMemorySnapshotRepository();

            Action save = async () => await repository.SaveSnapshot(new Order(new CreateOrder(Any.FullName())));

            save.ShouldThrow<DomainConfigurationException>();
        }
    }
}