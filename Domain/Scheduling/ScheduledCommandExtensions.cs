// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Its.Domain
{
    internal static class ScheduledCommandExtensions
    {
        public static bool IsDue(this IScheduledCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            return command.DueTime == null ||
                   command.DueTime <= Clock.Now();
        }

        public static ScheduledCommandResult Result<TAggregate>(
            this IScheduledCommand<TAggregate> scheduledCommand)
            where TAggregate : IEventSourced
        {
            var c = scheduledCommand as CommandScheduled<TAggregate>;

            if (c != null)
            {
                return c.Result;
            }

            return null;
        }

        public static void Result<TAggregate>(
            this IScheduledCommand<TAggregate> scheduledCommand,
            ScheduledCommandResult result)
            where TAggregate : IEventSourced
        {
            var c = scheduledCommand as CommandScheduled<TAggregate>;

            if (c != null)
            {
                c.Result = result;
            }
        }
    }
}