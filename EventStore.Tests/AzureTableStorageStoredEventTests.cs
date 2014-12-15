// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace Microsoft.Its.EventStore.Tests
{
    [TestFixture, TestClass]
    public class AzureTableStorageStoredEventTests
    {
        [Test]
        public void RowKey_alphabetic_sort_is_equivalent_to_numeric_descending_sort()
        {
            var events = new[]
            {
                new StoredEvent { Body = "a", RowKey = 1.ToRowKey() },
                new StoredEvent { Body = "b", RowKey = 2.ToRowKey() },
                new StoredEvent { Body = "c", RowKey = 10.ToRowKey() },
                new StoredEvent { Body = "d", RowKey = 11.ToRowKey() },
                new StoredEvent { Body = "e", RowKey = 100.ToRowKey() },
                new StoredEvent { Body = "f", RowKey = 1000.ToRowKey() },
                new StoredEvent { Body = "g", RowKey = long.MaxValue.ToRowKey() }
            };

            events.OrderBy(e => e.RowKey)
                  .Select(e => e.Body)
                  .Should()
                  .BeInDescendingOrder();
        }

        [Test]
        public void Setting_SequenceNumber_updates_RowKey()
        {
            var e = new StoredEvent { SequenceNumber = 1 };

            e.RowKey.Should().Be(1.ToRowKey());
        }

        [Test]
        public void Setting_RowKey_updates_SequenceNumber()
        {
            var e = new StoredEvent { RowKey = long.MaxValue.ToRowKey() };

            e.SequenceNumber.Should().Be(long.MaxValue);
        }
    }
}