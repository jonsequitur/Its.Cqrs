// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Its.Recipes;

namespace Microsoft.Its.Domain
{
    public static class CommandExtensions
    {
        public static bool CanBeDeliveredDuringSchedule(this ICommand command)
        {
            return command.IfTypeIs<ICommandSchedulingRules>()
                          .Then(c => c.CanBeDeliveredDuringSchedule)
                          .Else(() => true);
        }

        public static bool RequiresDurableScheduling(this ICommand command)
        {
            return command.IfTypeIs<ICommandSchedulingRules>()
                          .Then(c => c.RequiresDurableScheduling)
                          .Else(() => true);
        }
    }
}