using System;
using FluentAssertions;
using Microsoft.Its.Recipes;
using NUnit.Framework;

namespace Microsoft.Its.Domain.Testing.Tests
{
    [TestFixture]
    public class ClockTests
    {
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

            using (VirtualClock.Start(time))
            {
            }

            var now = Clock.Now();
            now.Should().BeInRange(now, DateTimeOffset.Now.AddMilliseconds(10));
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

        [Test]
        public void When_VirtualClock_Start_is_called_while_a_VirtualClock_is_already_in_use_it_throws()
        {
            VirtualClock.Start();
            Action startAgain = () => VirtualClock.Start();
            startAgain.ShouldThrow<InvalidOperationException>();
        }
    }
}