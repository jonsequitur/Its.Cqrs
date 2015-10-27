// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    }
}