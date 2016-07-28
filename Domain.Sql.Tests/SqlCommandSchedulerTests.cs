// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Its.Domain.Sql.CommandScheduler;

namespace Microsoft.Its.Domain.Sql.Tests
{
    public abstract class SqlCommandSchedulerTests
    {
        protected static string clockName =>
            Configuration.Current.Container.Resolve<GetClockName>()(null);

        public abstract Task When_a_clock_is_advanced_its_associated_commands_are_triggered();

        public abstract Task When_a_clock_is_advanced_then_commands_are_not_triggered_that_have_not_become_due();

        public abstract Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_domain_clock();

        public abstract Task Scheduled_commands_are_delivered_immediately_if_past_due_per_the_scheduler_clock();

        public abstract Task Scheduled_commands_with_no_due_time_are_delivered_at_Clock_Now_when_delivery_is_deferred();

        public abstract Task A_command_handler_can_request_retry_of_a_failed_command_as_soon_as_possible();

        public abstract Task A_command_handler_can_request_retry_of_a_failed_command_as_late_as_it_wants();

        public abstract Task A_command_handler_can_cancel_a_scheduled_command_after_it_fails();

        public abstract Task When_a_command_is_scheduled_but_an_exception_is_thrown_in_a_handler_then_an_error_is_recorded();

        public abstract Task When_a_command_is_scheduled_but_the_target_it_applies_to_is_not_found_then_the_command_is_retried();

        public abstract Task Constructor_commands_can_be_scheduled_to_create_new_aggregate_instances();

        public abstract Task When_a_constructor_command_fails_with_a_ConcurrencyException_it_is_not_retried();

        public abstract Task When_an_immediately_scheduled_command_depends_on_a_precondition_that_has_not_been_met_yet_then_there_is_not_initially_an_attempt_recorded();

        public abstract Task When_a_scheduled_command_depends_on_an_event_that_never_arrives_it_is_eventually_abandoned();

        public abstract Task When_command_is_durable_but_immediate_delivery_succeeds_then_it_is_not_redelivered();

        public abstract Task When_a_clock_is_advanced_and_a_command_fails_to_be_deserialized_then_other_commands_are_still_applied();

        public abstract Task Immediately_scheduled_commands_triggered_by_a_scheduled_command_have_their_due_time_set_to_the_causative_command_clock();

        public abstract Task When_a_clock_is_set_on_a_command_then_it_takes_precedence_over_default_clock();
    }
}