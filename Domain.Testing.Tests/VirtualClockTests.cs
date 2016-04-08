// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;
using Test.Domain.Ordering;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class VirtualClockTests
    {
        [SetUp]
        public void SetUp()
        {
            Command<Order>.AuthorizeDefault = (order, command) => true;
        }

        [TearDown]
        public void TearDown()
        {
            Clock.Reset();
        }

        [Test]
        public void VirtualClock_Start_can_be_used_to_specify_a_virtual_time_that_Clock_Now_will_return()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
                Clock.Now().Should().Be(time);
            }
        }

        [Test]
        public void When_VirtualClock_is_disposed_then_Clock_Current_is_restored_to_the_system_clock()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time.Subtract(60.Seconds())))
            {
            }

            var now = Clock.Now();
            now.Should().BeCloseTo(DateTimeOffset.UtcNow, 10);
        }

        [Test]
        public void VirtualClock_cannot_go_back_in_time_using_AdvanceTo()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
                Action goBackInTime = () => VirtualClock.Current.AdvanceTo(time + TimeSpan.FromSeconds(-1));
                goBackInTime.ShouldThrow<ArgumentException>();
            }
        }

        [Test]
        public void VirtualClock_cannot_go_back_in_time_using_AdvanceBy()
        {
            var time = Any.DateTimeOffset();

            using (VirtualClock.Start(time))
            {
                Action goBackInTime = () => VirtualClock.Current.AdvanceBy(TimeSpan.FromSeconds(-1));
                goBackInTime.ShouldThrow<ArgumentException>();
            }
        }
    }
}