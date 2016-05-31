// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Entity.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class ExceptionExtensionsTests
    {
        [Test]
        public void DbUpdateConcurrencyException_is_a_concurrency_exception()
        {
            var exception = new DbUpdateConcurrencyException("Cannot insert duplicate key");

            exception.IsConcurrencyException().Should().BeTrue();
        }

        [Test]
        public void An_exception_wrapping_a_concurrency_exception_is_a_concurrency_exception()
        {
            var exception =
                new AggregateException(
                    new DbUpdateConcurrencyException("Cannot insert duplicate key"));

            exception.IsConcurrencyException().Should().BeTrue();
        }
    }
}