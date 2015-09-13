// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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

            clock = clock ?? Clock.Current;

            return (command.DueTime == null ||
                    command.DueTime <= clock.Now())
                   && !(command.Result is CommandDelivered);
        }
    }
}