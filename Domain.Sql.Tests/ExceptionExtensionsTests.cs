// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Sql.Tests
{
    [TestFixture]
    public class ExceptionExtensionsTests
    {
        [Test]
        public void A_DbUpdateConcurrencyException_is_a_concurrency_exception()
        {
            var exception = new DbUpdateConcurrencyException();

            exception.IsConcurrencyException().Should().BeTrue();
        }

        [Test]
        public void A_insert_concurrency_DataException_is_a_concurrency_exception()
        {
            var exception = new DataException("Cannot insert duplicate key");

            exception.IsConcurrencyException().Should().BeTrue();
        }

        [Test]
        public void An_insert_concurrency_exception_wrapped_in_an_exception_is_a_concurrency_exception()
        {
            var exception =
                new AggregateException(
                    new DataException("Cannot insert duplicate key"));

            exception.IsConcurrencyException().Should().BeTrue();
        }

        [Test]
        public void An_insert_concurrency_exception_nested_deep_in_other_exceptions_is_a_concurrency_exception2()
        {
            var exception =
                new AggregateException(
                    new AggregateException(
                        new DataException("Cannot insert duplicate key")));

            exception.IsConcurrencyException().Should().BeTrue();
        }

        [Test]
        public void An_OptimisticConcurrencyException_is_a_concurrency_exception()
        {
            var exception = new OptimisticConcurrencyException();

            exception.IsConcurrencyException().Should().BeTrue();
        }
    }
}