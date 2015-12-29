// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class ScheduledCommandExtensions
    {
        public static bool IsDue(
            this IScheduledCommand command,
            IClock clock = null)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            clock = clock ??
                    command.Result
                           .IfTypeIs<CommandScheduled>()
                           .Then(scheduled => scheduled.Clock)
                           .ElseDefault() ??
                    Clock.Current;

            return (command.DueTime == null ||
                    command.DueTime <= clock.Now())
                   && !(command.Result is CommandDelivered);
        }

        public static async Task<bool> IsPreconditionSatisfied(
            this ICommandPreconditionVerifier preconditionVerifier,
            IScheduledCommand scheduledCommand)
        {
            if (preconditionVerifier == null)
            {
                throw new ArgumentNullException("preconditionVerifier");
            }

            var precondition = scheduledCommand.DeliveryPrecondition;

            if (precondition == null)
            {
                return true;
            }

            return await preconditionVerifier.HasBeenApplied(
                precondition.AggregateId, 
                precondition.ETag);
        }
    }
}