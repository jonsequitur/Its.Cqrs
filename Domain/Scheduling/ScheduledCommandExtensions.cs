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
    }
}