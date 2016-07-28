// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Tests
{
    public abstract class SchedulerClockTests
    {
        public abstract void A_clock_cannot_be_moved_to_a_prior_time();

        public abstract void Two_clocks_cannot_be_created_having_the_same_name();

        public abstract Task When_a_scheduler_clock_is_advanced_then_the_domain_clock_is_coordinated_to_the_scheduler_clock_for_events_written_as_a_result();
    }
}